# 图片预览

给扫码用的静态页。手机打开后只有背景、一块 16:9 的图，和「保存到本地」。没有输入框。Unity 把图片传到 OSS 后，把图片地址拼在这个网页后面，生成二维码。

## 拼接

网页地址末尾加 `?`，再接图片的完整地址：

```text
https://你的域名/index.html?https://bucket.oss-cn-hangzhou.aliyuncs.com/photo.jpg
```

签名链接里的 `&` 可以原样接在后面。也认 `?url=`、`?img=`、`?key=对象路径`，以及 `#` 后面直接接图片地址。

## 可以直接放到 OSS 吗

可以。上传 `index.html` 即可，OSS 只负责发这个文件。

阿里云 OSS 有一个限制：用默认域名（`*.aliyuncs.com`）访问 HTML 时，响应里会带 `Content-Disposition: attachment` 和 `x-oss-force-download: true`，浏览器会下载文件，而不是打开网页。绑定自定义域名后不再加这个头，页面才能直接显示。Bucket 在中国内地时，这个域名需要 ICP 备案。

### 阿里云上的做法

1. 创建 Bucket。需要公开访问时，先关掉「阻止公共访问」，再把读写权限设为公共读。这个 Bucket 里只放网页和可以公开的图片。
2. 上传 `index.html`。确认元数据里 `Content-Type` 是 `text/html`，不要把 `Content-Disposition` 设成 `attachment`。
3. 打开 **数据管理 → 静态页面**，默认首页填 `index.html`。这样访问域名根路径会打开本页。
4. 在 **域名管理** 绑定已备案的域名，并把它 CNAME 到 Bucket 的外网域名。用这个域名访问。
5. 二维码内容用 `https://你的域名/index.html?` 接上图片地址。

图片也尽量走自定义域名。部分地域的 OSS 默认域名会同样强制下载图片，`<img>` 会加载失败。

官方说明：[静态网站托管](https://help.aliyun.com/zh/oss/user-guide/hosting-static-websites)、[为什么默认域名会强制下载](https://help.aliyun.com/zh/oss/user-guide/0048-00000001)。

### 保存图片

- 图片和网页在同一个域名下时，点「保存到本地」直接下载原文件。iPhone 会弹出系统分享，从里面存到照片。
- 不在同一个域名时，浏览器要先把图片读下来，图片所在 Bucket 需要允许跨域：
  - 来源：网页域名，或 `*`
  - 方法：`GET`、`HEAD`
  - 允许的 Headers：`*`

私有 Bucket 的图片不要改成公共读，把签名 URL 贴进页面即可。签名过期后需要换一条新的。
