import type { Metadata } from "next";
import { headers } from "next/headers";
import "./globals.css";

export async function generateMetadata(): Promise<Metadata> {
  const incoming = await headers();
  const host = incoming.get("x-forwarded-host") ?? incoming.get("host") ?? "localhost:3000";
  const protocol = incoming.get("x-forwarded-proto") ?? (host.startsWith("localhost") ? "http" : "https");
  const origin = `${protocol}://${host}`;
  const socialImage = new URL("/og.png", origin).toString();

  return {
    title: "2026 电赛 C 题｜数字钥匙实验系统",
    description: "基于 UWB 定位、4 位身份验证、蓝牙声光控制与智能门锁的数字钥匙实验系统。",
    openGraph: {
      title: "2026 电赛 C 题｜数字钥匙实验系统",
      description: "UWB 定位 · 身份验证 · 蓝牙声光 · 智能门锁",
      type: "website",
      url: origin,
      images: [{ url: socialImage, width: 1745, height: 909, alt: "数字钥匙实验系统" }],
    },
    twitter: {
      card: "summary_large_image",
      title: "2026 电赛 C 题｜数字钥匙实验系统",
      description: "UWB 定位 · 身份验证 · 蓝牙声光 · 智能门锁",
      images: [socialImage],
    },
  };
}

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="zh-CN">
      <body>{children}</body>
    </html>
  );
}
