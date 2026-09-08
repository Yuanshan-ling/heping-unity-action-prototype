# Heping｜Unity 2D 动作游戏原型

基于 Unity 制作的 2D 动作游戏原型，重点实现战斗手感、角色反馈，以及任务、背包与 NPC 交互等完整玩法系统。

> **引擎版本：** Unity 6.3 LTS（6000.3.21f1）  
> **渲染管线：** URP 2D  
> **开发语言：** C#

## 演示视频

> 视频演示：待补充
> 
## 项目亮点

- 实现四段近战连击、蓄力攻击、冲刺、格挡与完美闪避。
- 实现命中反馈、能量、架势与 Boss 战斗交互等战斗反馈系统。
- 实现开场 QTE、剧情对话与 NPC 交互流程。
- 实现背包、装备、货币、商店与任务系统。
- 实现敌人巡逻、生成、特殊攻击与 Boss 行为逻辑。
- 实现传送、采集及场景交互等玩法功能。

## 打开项目

1. Clone 本仓库到本地。
2. 使用 **Unity Hub 6000.3.21f1** 打开项目根目录。
3. 项目会自动尝试打开主场景。
4. 如未自动打开，请手动双击：

   `Assets/Scenes/heping_Main.unity`

5. 在 Unity Editor 中点击 Play 运行。

## 项目结构

```text
Assets/
├── Scenes/        # 主游戏场景
├── Script/        # 角色、UI、战斗、任务、AI 等核心脚本
├── Script1/       # StarCore 战斗与 Boss 相关脚本
├── Prefabs/       # 游戏对象预制体
├── Animations/    # 动画片段与 Animator Controller
├── Items/         # 物品定义
└── Settings/      # URP 2D 渲染与项目设置
