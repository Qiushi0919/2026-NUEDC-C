# 数字钥匙实验系统项目网站

2026 全国大学生电子设计竞赛 C 题项目主页，展示软件运行状态、实物系统、功能闭环和资料入口。

## 本地运行

```powershell
pnpm install
pnpm run dev
```

默认预览地址为 `http://localhost:3000/`。

## 构建

```powershell
pnpm run build
```

网站使用 vinext 构建，并保留 Sites 所需的 `.openai/hosting.json` 配置。

GitHub Pages 发布版本使用 Next.js 静态导出：

```sh
NEXT_PUBLIC_BASE_PATH=/2026-NUEDC-C pnpm run build:github-pages
```

作者姓名旁的联系方式由 `app/components/AuthorContacts.tsx` 提供：邮箱、GitHub、微信、个人网站、谷歌学术。鼠标悬停或键盘聚焦显示说明，微信按钮打开个人主页已有的公开二维码。谷歌学术指向正式作者主页：https://scholar.google.com/citations?user=TkPyZ-UAAAAJ。

源码维护在 `main/website/`，当前 GitHub Pages 同时配置了 `gh-pages` 根目录作为发布入口。发布时保留主分支源码，并同步静态导出，避免后续构建覆盖联系信息。
