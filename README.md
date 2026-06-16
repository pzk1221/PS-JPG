# Photoshop 图层加背景批量导出 JPG

这是一个给 Adobe Photoshop 2020 使用的小工具。它可以把当前打开文档里的每个普通图层，分别与背景图层合成后导出为 JPG。

典型用途：一张产品图、专利图、外观图或设计稿里有多个视图图层，例如“主视图、左视图、右视图、俯视图、立体图”。运行脚本后，会自动导出：

- 背景 + 主视图
- 背景 + 左视图
- 背景 + 右视图
- 背景 + 俯视图
- 背景 + 立体图

导出的 JPG 文件名会直接使用 Photoshop 中的图层名称。

## 功能特点

- 双击 `.vbs` 文件即可运行。
- 不需要手动逐个隐藏/显示图层。
- 每张 JPG 都会保留背景图层。
- JPG 文件名自动使用图层名称。
- 图层重名时自动追加 `_2`、`_3`，避免覆盖。
- 导出到当前 Photoshop 文件的原始文件夹。
- 导出后自动恢复原来的图层可见状态。
- 不保存、不修改当前 Photoshop 文档。
- 支持 Photoshop 图层组中的普通图层。
- 没有标准 Background 图层时，会把最底部的普通图层当作背景。

## 文件说明

本工具主要由两个文件组成：

```text
run_export_layers_with_background.vbs
ps_export_layers_with_background.jsx
```

`run_export_layers_with_background.vbs` 是双击入口。它负责连接 Photoshop，并调用真正的 Photoshop 脚本。

`ps_export_layers_with_background.jsx` 是 Photoshop ExtendScript 脚本。它负责读取图层、切换可见性、导出 JPG、恢复现场。

这两个文件需要放在同一个文件夹里。

## 使用方法

1. 打开 Adobe Photoshop 2020。
2. 在 Photoshop 中打开要处理的 PSD、PNG 或其他图片文件。
3. 确认该文件来自磁盘上的某个文件夹。未保存的新建文件无法判断导出位置。
4. 双击 `run_export_layers_with_background.vbs`。
5. 等待提示完成。
6. 到原始图片所在文件夹查看导出的 JPG。

## 导出规则

脚本会按下面的逻辑处理：

1. 找到背景图层。
2. 记录所有图层当前的可见状态。
3. 隐藏所有图层。
4. 显示背景图层和当前要导出的普通图层。
5. 用当前图层名称导出 JPG。
6. 对下一个普通图层重复这个过程。
7. 全部导出后，恢复运行前的图层可见状态。

## 背景图层如何判断

脚本会按优先级查找背景：

1. Photoshop 标准的 Background 图层。
2. 名称为 `Background` 的普通图层。
3. 如果找不到以上图层，则使用最底部的普通图层作为背景。

如果你的背景层不是最底层，建议把背景层改成 Photoshop 的 Background 图层，或者把背景层移动到最底部。

## 文件名规则

导出文件名来自 Photoshop 图层名。

例如图层名为：

```text
主视图
左视图
右视图
立体图
```

导出后会得到：

```text
主视图.jpg
左视图.jpg
右视图.jpg
立体图.jpg
```

如果图层名里有 Windows 文件名不允许的字符，例如：

```text
\ / : * ? " < > |
```

脚本会自动替换成 `_`。

如果两个图层重名，例如都叫 `主视图`，会导出为：

```text
主视图.jpg
主视图_2.jpg
```

## JPG 质量

脚本使用 Photoshop 的 `JPEGSaveOptions` 导出，质量设置为 `12`，也就是 Photoshop 脚本接口里的最高 JPG 质量。

同时会嵌入当前文档的颜色配置文件，尽量保持颜色表现一致。

## 会不会改变原文件

不会。

脚本只是临时切换图层显示状态，然后使用 `saveAs` 的“作为副本”方式导出 JPG。导出结束后会恢复运行前的可见状态。

脚本不会保存当前 Photoshop 文档。

如果你的 Photoshop 标题栏上有 `*`，表示当前文件有未保存修改。运行脚本不会替你保存这些修改。

## 常见问题

### 双击后提示 Photoshop 忙碌

通常是 Photoshop 里还有弹窗没有关闭，例如保存提示、脚本警告、导出提示等。

处理方法：

1. 切回 Photoshop。
2. 关闭或确认所有弹窗。
3. 再双击运行脚本。

### 双击后提示找不到脚本文件

请确认这两个文件在同一个文件夹：

```text
run_export_layers_with_background.vbs
ps_export_layers_with_background.jsx
```

不要只复制 `.vbs`，否则它找不到真正执行的 `.jsx`。

### 没有导出到我想要的位置

脚本固定导出到当前 Photoshop 文档的原始文件夹。

例如你打开的是：

```text
D:\项目\图片\立体图.PNG
```

导出的 JPG 就会在：

```text
D:\项目\图片
```

### 背景不对

如果导出的图片背景不是你想要的背景，请检查：

- 背景图层是否是 Photoshop 标准 Background 图层。
- 或背景图层名称是否为 `Background`。
- 或背景图层是否位于最底部。

### 为什么不用中文文件名作为脚本入口

Windows Script Host 在某些系统上会用本机 ANSI 编码读取 `.vbs` 文件。如果 `.vbs` 文件里包含中文字符串，可能会出现乱码，甚至导致“语句未结束”等脚本错误。

所以本工具的可执行入口使用英文文件名和英文提示，避免编码问题。Photoshop 图层名称仍然可以是中文，导出的 JPG 文件名也可以是中文。

## 兼容性

已在以下环境验证：

- Windows
- Adobe Photoshop 2020
- Photoshop COM 自动化接口
- Photoshop ExtendScript JSX

其他 Photoshop 版本大概率也可以使用，但未逐一测试。

## 安全说明

这个工具只会：

- 读取当前 Photoshop 文档的图层结构。
- 临时改变图层可见性。
- 导出 JPG 文件到原始文件夹。
- 恢复图层可见性。

它不会：

- 上传文件。
- 删除文件。
- 保存或覆盖当前 Photoshop 源文件。
- 修改系统设置。

## 开源协议

MIT License。
