using UnityEngine;
using UnityEditor;
using System.IO;

public class SkillGeneratScripts : EditorWindow
{
    private string scriptName = "NewScript";
    private string savePath = "Assets/Scripts/Game/Skill/Logic";
    private string namespaceName = "FusionContinent.Skill";
    private bool useNamespace = true;

    // 技能设置
    private enum SkillType { [InspectorName("伤害")] Damage, [InspectorName("治疗")] Heal, [InspectorName("Buff")] Buff }
    private enum LaunchType { [InspectorName("指定位置")] TargetPosition, [InspectorName("指定方向")] TargetDirection, [InspectorName("不需要")] None }
    private enum TakeDamageType { [InspectorName("一次")] Once, [InspectorName("多次")] Multiple, [InspectorName("持续伤害")] Continuous }
    private SkillType skillType = SkillType.Damage;
    private LaunchType launchType = LaunchType.TargetPosition;
    private bool isBounce = false;
    private TakeDamageType takeDamageType = TakeDamageType.Once;
    private float damageInterval = 0f;

    [MenuItem("Tools/技能脚本生成器")]
    public static void ShowWindow()
    {
        var window = GetWindow<SkillGeneratScripts>("脚本生成器");
        window.minSize = new Vector2(900, 600);
    }

    private Vector2 leftScrollPosition;
    private Vector2 rightScrollPosition;

    private void OnGUI()
    {
        // 标题样式
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            margin = new RectOffset(0, 0, 10, 10)
        };

        EditorGUILayout.LabelField("📝 C# 技能脚本生成器", titleStyle);
        EditorGUILayout.Space(10);

        // 主布局：左右两栏
        EditorGUILayout.BeginHorizontal();

        // ========== 左侧栏：配置区域 ==========
        EditorGUILayout.BeginVertical(GUILayout.Width(350));
        leftScrollPosition = EditorGUILayout.BeginScrollView(leftScrollPosition, GUILayout.ExpandHeight(true));

        // 脚本名称输入
        EditorGUILayout.LabelField("脚本设置", EditorStyles.boldLabel);
        scriptName = EditorGUILayout.TextField("技能名称", scriptName);
        
        // 自动修正脚本名（移除空格和特殊字符）
        if (GUILayout.Button("🔄 修正命名规范", GUILayout.Height(25)))
        {
            scriptName = System.Text.RegularExpressions.Regex.Replace(scriptName, @"[^a-zA-Z0-9_]", "");
            if (char.IsDigit(scriptName[0])) scriptName = "_" + scriptName;
        }

        EditorGUILayout.Space(10);

        // 路径设置
        EditorGUILayout.LabelField("路径设置", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        savePath = EditorGUILayout.TextField("保存路径", savePath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("选择保存文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // 转换为相对路径
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    savePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 命名空间设置
        useNamespace = EditorGUILayout.Toggle("使用命名空间", useNamespace);
        if (useNamespace)
        {
            namespaceName = EditorGUILayout.TextField("命名空间", namespaceName);
        }

        EditorGUILayout.Space(10);

        // 技能设置
        EditorGUILayout.LabelField("技能设置", EditorStyles.boldLabel);
        skillType = (SkillType)EditorGUILayout.EnumPopup("技能类型", skillType);
        launchType = (LaunchType)EditorGUILayout.EnumPopup("技能发射类型", launchType);
        isBounce = EditorGUILayout.Toggle("能够反弹墙壁", isBounce);
        
        // 只有伤害类型才显示多种造成伤害选项
        if (skillType == SkillType.Damage)
        {
            takeDamageType = (TakeDamageType)EditorGUILayout.EnumPopup("造成伤害类型", takeDamageType);
            
            // 根据伤害类型显示不同参数
            switch (takeDamageType)
            {
                case TakeDamageType.Once:
                    break;
                case TakeDamageType.Multiple:
                    damageInterval = EditorGUILayout.FloatField("造成伤害间隔", damageInterval);
                    break;
                case TakeDamageType.Continuous:
                    damageInterval = EditorGUILayout.FloatField("造成伤害间隔", damageInterval);
                    break;
            }
        }

        EditorGUILayout.Space(20);

        // 生成按钮
        GUIStyle generateButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white, background = MakeTexture(2, 2, new Color(0.2f, 0.6f, 0.2f)) }
        };

        EditorGUILayout.BeginVertical(GUI.skin.box);
        if (GUILayout.Button("✨ 生成脚本", generateButtonStyle, GUILayout.Height(45)))
        {
            GenerateScript();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // 分隔线
        EditorGUILayout.BeginVertical(GUILayout.Width(2));
        GUILayout.Box("", GUILayout.Width(2), GUILayout.ExpandHeight(true));
        EditorGUILayout.EndVertical();

        // ========== 右侧栏：脚本预览 ==========
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        
        EditorGUILayout.LabelField("脚本预览", EditorStyles.boldLabel);
        
        rightScrollPosition = EditorGUILayout.BeginScrollView(rightScrollPosition, GUILayout.ExpandHeight(true));
        
        GUIStyle previewStyle = new GUIStyle(EditorStyles.textArea)
        {
            wordWrap = true,
            fontSize = 12,
            font = EditorStyles.standardFont
        };
        string previewContent = GenerateScriptContent();
        // 使用 LabelField 显示，高度根据内容自动计算，让 ScrollView 处理滚动
        EditorGUILayout.LabelField(previewContent, previewStyle, GUILayout.ExpandWidth(true));

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private string GenerateScriptContent()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine("using Config;");
        sb.AppendLine("using DG.Tweening;");
        sb.AppendLine("using FusionContinent;");
        sb.AppendLine("using FusionContinent.Skill;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        
        // 判断模板类型
        bool isPositionTemplate = skillType == SkillType.Damage && launchType == LaunchType.TargetPosition;  // 定点范围伤害
        bool isDirectionTemplate = skillType == SkillType.Damage && launchType == LaunchType.TargetDirection; // 定向投射物
        
        if (useNamespace && !string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {scriptName}Skill : SkillBase");
            sb.AppendLine("    {");
            
            if (isPositionTemplate)
            {
                // 生成定点范围伤害代码（DrawSlashBullet）
                sb.AppendLine("        private float _localScale;");
                sb.AppendLine("        private float _skillDuration;");
                sb.AppendLine("        private Vector3 _targetPos;");
                sb.AppendLine();
                sb.AppendLine("        protected override void OnCast(object obj)");
                sb.AppendLine("        {");
                sb.AppendLine("            _targetPos = (Vector3)obj;");
                sb.AppendLine("            _localScale = 100f + Attributes.GetFinal(AttributeType.SkillRange);");
                sb.AppendLine("            _skillDuration = Attributes.GetFinal(AttributeType.SkillDuration);");
                sb.AppendLine("            SpawnEffect();");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        private void SpawnEffect()");
                sb.AppendLine("        {");
                sb.AppendLine("            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(Config.ProjectileName);");
                sb.AppendLine("            if (pool == null) return;");
                sb.AppendLine();
                sb.AppendLine("            GameObject effect = pool.Get();");
                sb.AppendLine("            if (effect == null) return;");
                sb.AppendLine();
                sb.AppendLine("            effect.transform.SetParent(GameLand.Default.Bullet);");
                sb.AppendLine("            effect.transform.position = _targetPos;");
                sb.AppendLine("            effect.transform.localScale = Vector3.one * _localScale;");
                sb.AppendLine();
                sb.AppendLine("            var slashBullet = effect.GetComponent<DrawSlashBullet>();");
                sb.AppendLine("            if (slashBullet != null)");
                sb.AppendLine("            {");
                sb.AppendLine("                slashBullet.repelMonsterVal = Attributes.GetFinal(AttributeType.SkillRepelMonster);");
                sb.AppendLine("                slashBullet.knockUpVal = Attributes.GetFinal(AttributeType.SkillKnockUpMonster);");
                sb.AppendLine($"                slashBullet.SetStartAttack(this, {damageInterval}f);");
                sb.AppendLine("            }");
                sb.AppendLine();
                sb.AppendLine("            DOVirtual.DelayedCall(_skillDuration, () =>");
                sb.AppendLine("            {");
                sb.AppendLine("                if (effect != null) pool.Return(effect);");
                sb.AppendLine("            });");
                sb.AppendLine("        }");
            }
            else if (isDirectionTemplate)
            {
                // 生成定向投射物代码（ProjectileBullet）- 参考 FireballSkill
                sb.AppendLine("        private float _localScale;");
                sb.AppendLine("        private float _missileSpeed;");
                sb.AppendLine("        private float _duration;");
                sb.AppendLine();
                sb.AppendLine("        protected override void OnCast(object obj)");
                sb.AppendLine("        {");
                sb.AppendLine("            // 读取属性");
                sb.AppendLine("            _localScale = 1f + Attributes.GetFinal(AttributeType.SkillRange);");
                sb.AppendLine("            _missileSpeed = Attributes.GetFinal(AttributeType.SkillMissileSpeed);");
                sb.AppendLine("            _duration = Attributes.GetFinal(AttributeType.SkillDuration);");
                sb.AppendLine();
                sb.AppendLine("            // 获取发射方向");
                sb.AppendLine("            Vector3 direction = ((Vector3)obj).normalized;");
                sb.AppendLine();
                sb.AppendLine("            // 发射子弹");
                sb.AppendLine("            SpawnProjectile(direction);");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        private void SpawnProjectile(Vector3 direction)");
                sb.AppendLine("        {");
                sb.AppendLine("            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(Config.ProjectileName);");
                sb.AppendLine("            if (pool == null)");
                sb.AppendLine("            {");
                sb.AppendLine($"                Debug.LogError($\"{scriptName}Skill: 找不到对象池 {{Config.ProjectileName}}\");");
                sb.AppendLine("                return;");
                sb.AppendLine("            }");
                sb.AppendLine();
                sb.AppendLine("            GameObject bullet = pool.Get();");
                sb.AppendLine("            if (bullet == null) return;");
                sb.AppendLine();
                sb.AppendLine("            // 设置初始位置和大小");
                sb.AppendLine("            bullet.transform.SetParent(GameLand.Default.Bullet);");
                sb.AppendLine("            bullet.transform.position = Parent.transform.position;");
                sb.AppendLine("            bullet.transform.localScale = Vector3.one * _localScale;");
                sb.AppendLine();
                sb.AppendLine("            // 使用 ProjectileBullet 组件");
                sb.AppendLine("            var projectile = bullet.GetComponent<ProjectileBullet>();");
                sb.AppendLine("            if (projectile != null)");
                sb.AppendLine("            {");
                sb.AppendLine("                projectile.SetStart(this, direction.normalized, 0f);");
                sb.AppendLine("                projectile.SetPool(pool);");
                sb.AppendLine();
                sb.AppendLine("                projectile.OnHit += (hitPos) =>");
                sb.AppendLine("                {");
                sb.AppendLine("                    OnProjectileHit(hitPos);");
                sb.AppendLine("                };");
                sb.AppendLine();
                sb.AppendLine("                projectile.OnComplete += () =>");
                sb.AppendLine("                {");
                sb.AppendLine("                    if (bullet != null && bullet.activeInHierarchy)");
                sb.AppendLine("                    {");
                sb.AppendLine("                        pool.Return(bullet);");
                sb.AppendLine("                    }");
                sb.AppendLine("                };");
                sb.AppendLine("            }");
                sb.AppendLine("            else");
                sb.AppendLine("            {");
                sb.AppendLine("                // 备用：DOTween移动");
                sb.AppendLine($"                Debug.LogWarning(\"{scriptName}Skill: 预制体缺少 ProjectileBullet 组件\");");
                sb.AppendLine();
                sb.AppendLine("                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;");
                sb.AppendLine("                bullet.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);");
                sb.AppendLine();
                sb.AppendLine("                Vector3 targetPos = Parent.transform.position + direction * 100f;");
                sb.AppendLine("                bullet.transform.DOMove(targetPos, 100f / _missileSpeed)");
                sb.AppendLine("                    .SetEase(Ease.Linear)");
                sb.AppendLine("                    .SetSpeedBased()");
                sb.AppendLine("                    .OnComplete(() => pool.Return(bullet));");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        private void OnProjectileHit(Vector3 hitPos)");
                sb.AppendLine("        {");
                sb.AppendLine("            // 播放命中特效");
                sb.AppendLine("            if (!string.IsNullOrEmpty(Config.SkillHitEffect))");
                sb.AppendLine("            {");
                sb.AppendLine("                ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()");
                sb.AppendLine("                {");
                sb.AppendLine("                    PoolName = Config.SkillHitEffect,");
                sb.AppendLine("                    ParentTran = GameLand.Default.DamageParent.transform,");
                sb.AppendLine("                    Position = hitPos,");
                sb.AppendLine("                    LocalScale = 150f,");
                sb.AppendLine("                    DelayReturn = 0.5f");
                sb.AppendLine("                });");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
            }
            else
            {
                // 生成简化代码
                sb.AppendLine("        protected override void OnCast(object obj)");
                sb.AppendLine("        {");
                sb.AppendLine("            ");
                sb.AppendLine("        }");
            }
            sb.AppendLine();
            sb.AppendLine($"        private void {scriptName}(Vector3 v)");
            sb.AppendLine("        {");
            sb.AppendLine("            ");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
        else
        {
            sb.AppendLine($"public class {scriptName}Skill : SkillBase");
            sb.AppendLine("{");
            
            if (isPositionTemplate)
            {
                // 生成定点范围伤害代码（无命名空间）
                sb.AppendLine("    private float _localScale;");
                sb.AppendLine("    private float _skillDuration;");
                sb.AppendLine("    private Vector3 _targetPos;");
                sb.AppendLine();
                sb.AppendLine("    protected override void OnCast(object obj)");
                sb.AppendLine("    {");
                sb.AppendLine("        _targetPos = (Vector3)obj;");
                sb.AppendLine("        _localScale = 100f + Attributes.GetFinal(AttributeType.SkillRange);");
                sb.AppendLine("        _skillDuration = Attributes.GetFinal(AttributeType.SkillDuration);");
                sb.AppendLine("        SpawnEffect();");
                sb.AppendLine("    }");
                sb.AppendLine();
                sb.AppendLine("    private void SpawnEffect()");
                sb.AppendLine("    {");
                sb.AppendLine("        var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(Config.ProjectileName);");
                sb.AppendLine("        if (pool == null) return;");
                sb.AppendLine();
                sb.AppendLine("        GameObject effect = pool.Get();");
                sb.AppendLine("        if (effect == null) return;");
                sb.AppendLine();
                sb.AppendLine("        effect.transform.SetParent(GameLand.Default.Bullet);");
                sb.AppendLine("        effect.transform.position = _targetPos;");
                sb.AppendLine("        effect.transform.localScale = Vector3.one * _localScale;");
                sb.AppendLine();
                sb.AppendLine("        var slashBullet = effect.GetComponent<DrawSlashBullet>();");
                sb.AppendLine("        if (slashBullet != null)");
                sb.AppendLine("        {");
                sb.AppendLine("            slashBullet.repelMonsterVal = Attributes.GetFinal(AttributeType.SkillRepelMonster);");
                sb.AppendLine("            slashBullet.knockUpVal = Attributes.GetFinal(AttributeType.SkillKnockUpMonster);");
                sb.AppendLine($"            slashBullet.SetStartAttack(this, {damageInterval}f);");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        DOVirtual.DelayedCall(_skillDuration, () =>");
                sb.AppendLine("        {");
                sb.AppendLine("            if (effect != null) pool.Return(effect);");
                sb.AppendLine("        });");
                sb.AppendLine("    }");
            }
            else if (isDirectionTemplate)
            {
                // 生成定向投射物代码（无命名空间）- 参考 FireballSkill
                sb.AppendLine("    private float _localScale;");
                sb.AppendLine("    private float _missileSpeed;");
                sb.AppendLine("    private float _duration;");
                sb.AppendLine();
                sb.AppendLine("    protected override void OnCast(object obj)");
                sb.AppendLine("    {");
                sb.AppendLine("        // 读取属性");
                sb.AppendLine("        _localScale = 1f + Attributes.GetFinal(AttributeType.SkillRange);");
                sb.AppendLine("        _missileSpeed = Attributes.GetFinal(AttributeType.SkillMissileSpeed);");
                sb.AppendLine("        _duration = Attributes.GetFinal(AttributeType.SkillDuration);");
                sb.AppendLine();
                sb.AppendLine("        // 获取发射方向");
                sb.AppendLine("        Vector3 direction = ((Vector3)obj).normalized;");
                sb.AppendLine();
                sb.AppendLine("        // 发射子弹");
                sb.AppendLine("        SpawnProjectile(direction);");
                sb.AppendLine("    }");
                sb.AppendLine();
                sb.AppendLine("    private void SpawnProjectile(Vector3 direction)");
                sb.AppendLine("    {");
                sb.AppendLine("        var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(Config.ProjectileName);");
                sb.AppendLine("        if (pool == null)");
                sb.AppendLine("        {");
                sb.AppendLine($"            Debug.LogError($\"{scriptName}Skill: 找不到对象池 {{Config.ProjectileName}}\");");
                sb.AppendLine("            return;");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        GameObject bullet = pool.Get();");
                sb.AppendLine("        if (bullet == null) return;");
                sb.AppendLine();
                sb.AppendLine("        // 设置初始位置和大小");
                sb.AppendLine("        bullet.transform.SetParent(GameLand.Default.Bullet);");
                sb.AppendLine("        bullet.transform.position = Parent.transform.position;");
                sb.AppendLine("        bullet.transform.localScale = Vector3.one * _localScale;");
                sb.AppendLine();
                sb.AppendLine("        // 使用 ProjectileBullet 组件");
                sb.AppendLine("        var projectile = bullet.GetComponent<ProjectileBullet>();");
                sb.AppendLine("        if (projectile != null)");
                sb.AppendLine("        {");
                sb.AppendLine("            projectile.SetStart(this, direction.normalized, 0f);");
                sb.AppendLine("            projectile.SetPool(pool);");
                sb.AppendLine();
                sb.AppendLine("            projectile.OnHit += (hitPos) =>");
                sb.AppendLine("            {");
                sb.AppendLine("                OnProjectileHit(hitPos);");
                sb.AppendLine("            };");
                sb.AppendLine();
                sb.AppendLine("            projectile.OnComplete += () =>");
                sb.AppendLine("            {");
                sb.AppendLine("                if (bullet != null && bullet.activeInHierarchy)");
                sb.AppendLine("                {");
                sb.AppendLine("                    pool.Return(bullet);");
                sb.AppendLine("                }");
                sb.AppendLine("            };");
                sb.AppendLine("        }");
                sb.AppendLine("        else");
                sb.AppendLine("        {");
                sb.AppendLine("            // 备用：DOTween移动");
                sb.AppendLine($"            Debug.LogWarning(\"{scriptName}Skill: 预制体缺少 ProjectileBullet 组件\");");
                sb.AppendLine();
                sb.AppendLine("            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;");
                sb.AppendLine("            bullet.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);");
                sb.AppendLine();
                sb.AppendLine("            Vector3 targetPos = Parent.transform.position + direction * 100f;");
                sb.AppendLine("            bullet.transform.DOMove(targetPos, 100f / _missileSpeed)");
                sb.AppendLine("                .SetEase(Ease.Linear)");
                sb.AppendLine("                .SetSpeedBased()");
                sb.AppendLine("                .OnComplete(() => pool.Return(bullet));");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine();
                sb.AppendLine("    private void OnProjectileHit(Vector3 hitPos)");
                sb.AppendLine("    {");
                sb.AppendLine("        // 播放命中特效");
                sb.AppendLine("        if (!string.IsNullOrEmpty(Config.SkillHitEffect))");
                sb.AppendLine("        {");
                sb.AppendLine("            ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()");
                sb.AppendLine("            {");
                sb.AppendLine("                PoolName = Config.SkillHitEffect,");
                sb.AppendLine("                ParentTran = GameLand.Default.DamageParent.transform,");
                sb.AppendLine("                Position = hitPos,");
                sb.AppendLine("                LocalScale = 150f,");
                sb.AppendLine("                DelayReturn = 0.5f");
                sb.AppendLine("            });");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
            }
            else
            {
                // 生成简化代码（无命名空间）
                sb.AppendLine("    protected override void OnCast(object obj)");
                sb.AppendLine("    {");
                sb.AppendLine("        ");
                sb.AppendLine("    }");
            }
            sb.AppendLine();
            sb.AppendLine($"    private void {scriptName}(Vector3 v)");
            sb.AppendLine("    {");
            sb.AppendLine("        ");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private void GenerateScript()
    {
        // 验证脚本名
        if (string.IsNullOrWhiteSpace(scriptName))
        {
            EditorUtility.DisplayDialog("错误", "脚本名称不能为空！", "确定");
            return;
        }

        // 确保路径存在
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        string fullPath = Path.Combine(savePath, scriptName+ "Skill" + ".cs");

        // 检查文件是否已存在
        if (File.Exists(fullPath))
        {
            if (!EditorUtility.DisplayDialog("文件已存在", $"脚本 {scriptName}Skill.cs 已存在，是否覆盖？", "覆盖", "取消"))
            {
                return;
            }
        }

        try
        {
            // 写入文件
            File.WriteAllText(fullPath, GenerateScriptContent());
            
            // 刷新Unity资源数据库
            AssetDatabase.Refresh();
            
            // 高亮显示生成的文件
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(fullPath);
            EditorUtility.DisplayDialog("成功", $"脚本已生成：\n{fullPath}", "确定");
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"生成失败：\n{e.Message}", "确定");
        }
    }

    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pixels);
        result.Apply();
        return result;
    }
}