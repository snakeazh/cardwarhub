#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
UI迁移助手 - 自动生成新框架代码
使用方式: python ui_migration_helper.py <PanelName> <Module> <PrefabPath>
示例: python ui_migration_helper.py LoginPanel01 Login UI/Login1
"""

import sys
import os
import json
from datetime import datetime
from string import Template

# 模板定义
VIEWMODEL_TEMPLATE = """using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using UIFramework.Services;

namespace Game.${Module}
{
    /// <summary>
    /// ${PanelName} ViewModel
    /// </summary>
    public class ${PanelName}ViewModel : ViewModel
    {
        #region 属性
        // TODO: 添加需要绑定的属性
        // public BindableProperty<string> Property1 { get; } = new BindableProperty<string>("");
        // public BindableProperty<bool> Property2 { get; } = new BindableProperty<bool>(false);
        #endregion

        #region 命令
        // TODO: 添加命令
        // public Command OnCommand1 { get; private set; }
        #endregion

        #region 依赖
        // TODO: 添加Service依赖
        // private ${Module}Service mService;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            // TODO: 获取Service
            // mService = GameServiceManager.Get<${Module}Service>();

            // TODO: 初始化命令
            // OnCommand1 = new Command(OnMethod1);

            // TODO: 加载数据
            LoadData();
            
            Debug.Log("[${PanelName}ViewModel] 创建完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Debug.Log("[${PanelName}ViewModel] 销毁");
        }

        private void LoadData()
        {
            // TODO: 从Service或PlayerPrefs加载数据
        }

        // TODO: 实现命令方法
        // private void OnMethod1()
        // {
        //     // 命令实现
        // }

        #region 事件
        // TODO: 添加事件
        // public System.Action OnEvent1;
        #endregion
    }
}
"""

PANEL_TEMPLATE = """using Game.${Module};
using UIFramework;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent
{
    /// <summary>
    /// ${PanelName} - 新框架版本
    /// 复用原${PanelName}的UI组件
    /// </summary>
    [PrefabPath("${PrefabPath}")]
    public class ${PanelName}New : UIPanel<${PanelName}ViewModel>
    {
        // 引用原Panel，复用其UI组件
        private ${PanelName} mOldPanel;

        void Awake()
        {
            mOldPanel = GetComponent<${PanelName}>();
            if (mOldPanel == null)
            {
                Debug.LogError("[${PanelName}New] 找不到 ${PanelName} 组件！请确保预制体上同时挂载了两个脚本");
                return;
            }
            
            Debug.Log("[${PanelName}New] 已关联 ${PanelName}");
        }

        protected override void OnBindViewModel(${PanelName}ViewModel vm)
        {
            if (mOldPanel == null) return;
            
            // TODO: 绑定属性
            // vm.Property1.OnValueChanged += (oldVal, newVal) =>
            // {
            //     mOldPanel.TextComponent.text = newVal;
            // };
            
            // TODO: 绑定按钮
            // mOldPanel.ButtonComponent.onClick.AddListener(() => vm.OnCommand1.Execute());
        }

        protected override void OnUnbindViewModel(${PanelName}ViewModel vm)
        {
            if (mOldPanel == null) return;
            
            // TODO: 清理监听
            // mOldPanel.ButtonComponent.onClick.RemoveAllListeners();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            // TODO: 面板打开逻辑
        }

        protected override void OnShow()
        {
            // TODO: 面板显示逻辑
        }

        protected override void OnHide()
        {
            // TODO: 面板隐藏逻辑
        }
    }
}
"""

SERVICE_TEMPLATE = """using UIFramework.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.${Module}
{
    /// <summary>
    /// ${Module}服务
    /// </summary>
    public class ${Module}Service : GameServiceBase
    {
        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            Debug.Log("[${Module}Service] 初始化完成");
        }

        // TODO: 添加业务逻辑方法
    }
}
"""


def generate_files(panel_name, module, prefab_path):
    """生成迁移所需的文件"""

    # 准备变量
    variables = {
        "PanelName": panel_name,
        "Module": module,
        "PrefabPath": prefab_path,
        "Date": datetime.now().strftime("%Y-%m-%d"),
    }

    # 文件路径
    base_path = os.path.join("Assets", "Scripts")

    files_to_create = [
        {
            "path": os.path.join(
                base_path, "Game", module, "ViewModels", f"{panel_name}ViewModel.cs"
            ),
            "content": Template(VIEWMODEL_TEMPLATE).substitute(variables),
            "description": "ViewModel文件",
        },
        {
            "path": os.path.join(
                base_path, "UI", prefab_path.replace("/", os.sep), f"{panel_name}New.cs"
            ),
            "content": Template(PANEL_TEMPLATE).substitute(variables),
            "description": "新Panel文件",
        },
        {
            "path": os.path.join(
                base_path, "Game", module, "Services", f"{module}Service.cs"
            ),
            "content": Template(SERVICE_TEMPLATE).substitute(variables),
            "description": "Service文件（如不存在）",
        },
    ]

    # 创建文件
    created_files = []
    for file_info in files_to_create:
        # 确保目录存在
        os.makedirs(os.path.dirname(file_info["path"]), exist_ok=True)

        # 如果文件已存在，添加.new后缀
        final_path = file_info["path"]
        if os.path.exists(final_path):
            final_path = final_path + ".new"
            print(f"⚠️  {file_info['description']}已存在，创建为: {final_path}")
        else:
            print(f"✅ 创建{file_info['description']}: {file_info['path']}")

        # 写入文件
        with open(final_path, "w", encoding="utf-8") as f:
            f.write(file_info["content"])

        created_files.append(final_path)

    return created_files


def print_checklist(panel_name, module, prefab_path):
    """打印迁移检查清单"""
    print("\n" + "=" * 60)
    print("📝 迁移检查清单")
    print("=" * 60)

    checklist = f"""
步骤1: 分析原界面
  [ ] 读取 Assets/Scripts/UI/{prefab_path}/{panel_name}.cs
  [ ] 列出所有UI组件（Button、Text、InputField等）
  [ ] 识别数据字段
  [ ] 识别业务逻辑

步骤2: 完善ViewModel
  [ ] 编辑 Assets/Scripts/Game/{module}/ViewModels/{panel_name}ViewModel.cs
  [ ] 添加BindableProperty属性
  [ ] 添加Command命令
  [ ] 实现OnCreate初始化

步骤3: 完善Service（如需要）
  [ ] 编辑 Assets/Scripts/Game/{module}/Services/{module}Service.cs
  [ ] 迁移业务逻辑
  [ ] 在GameMainController中注册Service

步骤4: 完善新Panel
  [ ] 编辑 Assets/Scripts/UI/{prefab_path}/{panel_name}New.cs
  [ ] 在Awake中获取原Panel组件
  [ ] 在OnBindViewModel中绑定数据
  [ ] 实现生命周期方法

步骤5: 配置预制体
  [ ] 在Unity中选中预制体
  [ ] 添加{panel_name}New组件
  [ ] 检查层级配置

步骤6: 修改打开方式
  [ ] 搜索所有打开该界面的代码
  [ ] 替换为UIManager.OpenPanel方式

步骤7: 测试验证
  [ ] 界面能正常打开关闭
  [ ] 数据绑定正常
  [ ] 按钮响应正常
  [ ] 无报错
"""
    print(checklist)

    print("\n" + "=" * 60)
    print("💡 提示")
    print("=" * 60)
    print(f"""
1. 新旧界面可以共存，逐步替换
2. 保持原{panel_name}的UI组件命名不变
3. 在{panel_name}New.cs中通过mOldPanel.xxx访问原组件
4. 测试通过后再删除原{panel_name}.cs
""")


def main():
    if len(sys.argv) < 3:
        print(
            "使用方式: python ui_migration_helper.py <PanelName> <Module> [PrefabPath]"
        )
        print("示例: python ui_migration_helper.py LoginPanel01 Login UI/Login1")
        print("示例: python ui_migration_helper.py ShopPanel Shop UI/Shop")
        sys.exit(1)

    panel_name = sys.argv[1]
    module = sys.argv[2]
    prefab_path = sys.argv[3] if len(sys.argv) > 3 else panel_name

    print(f"\n🚀 UI迁移助手 - 生成 {panel_name} 的新框架代码\n")
    print(f"Panel名称: {panel_name}")
    print(f"模块名称: {module}")
    print(f"预制体路径: {prefab_path}\n")

    # 生成文件
    created_files = generate_files(panel_name, module, prefab_path)

    # 打印检查清单
    print_checklist(panel_name, module, prefab_path)

    print(f"\n✨ 完成！生成了 {len(created_files)} 个文件")
    print("请按照检查清单逐步完成迁移。\n")


if __name__ == "__main__":
    main()
