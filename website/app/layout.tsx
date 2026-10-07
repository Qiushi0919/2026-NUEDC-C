import type { Metadata } from "next";
import "./globals.css";

const siteUrl = "https://qiushi0919.github.io/2026-NUEDC-C/";
const pageTitle = "谢秋实 · 电赛C题2026｜基于无线通信的数字钥匙实验系统";

export const metadata: Metadata = {
  metadataBase: new URL(siteUrl),
  title: pageTitle,
  authors: [{ name: "谢秋实（Qiushi Xie）", url: "https://qiushi0919.cn/" }],
  alternates: { canonical: siteUrl },
  description: "基于 UWB 定位、4 位身份验证、蓝牙声光控制与智能门锁的数字钥匙实验系统。",
  openGraph: {
    title: pageTitle,
    siteName: "Qiushi Xie",
    description: "UWB 定位 · 身份验证 · 蓝牙声光 · 智能门锁",
    type: "website",
    url: siteUrl,
    images: [{ url: "og.png", width: 1745, height: 909, alt: "数字钥匙实验系统" }],
  },
  twitter: {
    card: "summary_large_image",
    title: pageTitle,
    description: "UWB 定位 · 身份验证 · 蓝牙声光 · 智能门锁",
    images: ["og.png"],
  },
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="zh-CN">
      <body>{children}</body>
    </html>
  );
}
