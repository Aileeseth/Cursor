# Unity + ComfyUI 草图精绘对接

展览流程：**用户画完 → 点确认 → Unity 调本地 ComfyUI → 大屏显示美化图**。

不实时生成，降低 GPU 压力。

---

## 目录结构

```text
unity/SketchToImage/
├── Scripts/
│   ├── ComfyUI/
│   │   ├── ComfyUISettings.cs      # 配置 ScriptableObject
│   │   ├── ComfyUIClient.cs        # HTTP API 客户端
│   │   └── ComfyUIWorkflowRunner.cs # 上传草图 + 跑工作流
│   └── UI/
│       ├── SketchPad.cs            # 画板
│       ├── ExhibitionController.cs # 流程控制
│       └── ProjectorDisplayActivator.cs # 第二块屏（投影）
└── README.md
```

---

## 一、Unity 工程准备

### 1. 创建或打开 Unity 项目

推荐 **Unity 2021.3 LTS** 或 **2022.3 LTS**。

### 2. 复制脚本

把 `Scripts/` 整个文件夹复制到：

```text
Assets/SketchToImage/Scripts/
```

### 3. 允许 HTTP（本地 ComfyUI）

`Edit` → `Project Settings` → `Player` → `Other Settings`：

- 勾选 **Allow downloads over HTTP**（Unity 2022+）
- 或在 `Android/iOS` 相关设置里允许明文 HTTP（若打包到设备）

编辑器里访问 `http://127.0.0.1:8188` 一般可直接用。

---

## 二、场景搭建

### 层级建议

```text
Canvas (Drawing UI)
├── SketchPadArea (RawImage + SketchPad 组件)
├── BtnConfirm
├── BtnClear
└── BtnHome

Canvas (Big Screen)  或单独 Display 1
└── BigScreen (RawImage)

GeneratingPanel (loading 动画，默认隐藏)
ResultPanel (可选)

Managers
├── ComfyUIWorkflowRunner
└── ExhibitionController
```

### 组件绑定

| 对象 | 组件 | 说明 |
|------|------|------|
| 画板 RawImage | `SketchPad` | 拖 canvasImage 到自己 |
| Manager | `ComfyUIWorkflowRunner` | 挂 Runner |
| Manager | `ExhibitionController` | 绑定下面引用 |

**ExhibitionController 引用：**

- Sketch Pad → 画板上的 `SketchPad`
- Workflow Runner → `ComfyUIWorkflowRunner`
- Big Screen Image → 大屏 `RawImage`
- Confirm / Clear / Home → 对应按钮
- Drawing / Generating / Result Panel → 三个面板（可空）

---

## 三、ComfyUI 工作流导出

### 1. 在 ComfyUI 里搭好 SDXL Turbo + ControlNet 工作流（你已搭好）

### 2. 导出 API JSON

菜单：**Save (API Format)** → 保存为 `workflow_api.json`

### 3. 放进 Unity

```text
Assets/StreamingAssets/workflow_api.json
```

### 4. 查节点 ID

打开 `workflow_api.json`，找各节点的数字 id，例如：

```json
"14": {
  "class_type": "LoadImage",
  "inputs": { "image": "example.png" }
}
```

记下：

| 节点 | 你的 id（示例） |
|------|----------------|
| Load Image | `14` |
| CLIP 正向 | `12` |
| CLIP 反向 | `13` |

### 5. 创建配置资源

`Assets` 右键 → `Create` → `SketchToImage` → `ComfyUI Settings`

| 字段 | 值 |
|------|-----|
| Base Url | `http://127.0.0.1:8188` |
| Workflow Json | 拖入 `workflow_api.json` |
| Load Image Node Id | `14`（按你的 JSON 改） |
| Sketch Size | `512` |

把该 Settings 拖到 `ComfyUIWorkflowRunner` 的 Settings 槽。

---

## 四、运行前检查

1. **ComfyUI 已启动**

```cmd
python main.py --listen 127.0.0.1 --port 8188
```

2. 浏览器能打开 `http://127.0.0.1:8188`

3. Unity 里 `workflow_api.json` 节点 id 与 ComfyUI 一致

4. KSampler：**steps 4~8，cfg 1~2**（SDXL Turbo）

---

## 五、流程说明

```text
Drawing
  用户画草图
  点 Confirm
    ↓
Generating
  Unity 上传 PNG → ComfyUI
  轮询 history → 下载结果
    ↓
ShowingResult
  大屏显示美化图
    ↓
Home
  清空画板 + 清空大屏
```

失败时：打日志，回到 Drawing，不卡死。

---

## 六、多屏投影（可选）

场景里加空物体，挂 `ProjectorDisplayActivator`，`displayIndex = 1`。

Windows 显示设置里扩展显示器，投影接 Display 1。

---

## 七、常见问题

| 问题 | 处理 |
|------|------|
| Connection refused | ComfyUI 没启动或端口不对 |
| Node id not found | 重新导出 workflow，改 Settings 里的 id |
| 生成很慢 | 确认用 SDXL Turbo 参数，分辨率 512 |
| 结果和草图无关 | ControlNet 强度调到 0.8，确认是 Scribble 模型 |
| 大屏没图 | 检查 ExhibitionController 的 Big Screen 引用 |

---

## 八、下一步可扩展

- [ ] 自动 Prompt（接视觉模型 API）
- [ ] 二维码页（生成成功后再显示）
- [ ] 超时 / 重试 UI 文案
- [ ] 竖屏草图 512×768

---

## 九、API 说明（供调试）

Unity 调用的本地接口：

| 接口 | 方法 |
|------|------|
| `/upload/image` | POST 上传草图 |
| `/prompt` | POST 触发工作流 |
| `/history/{id}` | GET 取结果 |
| `/view?filename=...` | GET 下载图片 |

与浏览器里手动 Queue Prompt 是同一套服务。
