"use client";

import { useEffect, useRef, useState } from "react";

const basePath = process.env.NEXT_PUBLIC_BASE_PATH ?? "";
const asset = (path: string) => `${basePath}${path}`;

const features = [
  {
    number: "01",
    title: "钥匙身份广播与识别",
    copy: "数字钥匙持续发送 4 位身份 ID，门锁在感应区完成接收、比对与显示。",
    detail: "无线身份信标 → UWB 基站 → 上位机验证",
    video: asset("/videos/feature-1.mp4"),
    poster: asset("/videos/feature-1.jpg"),
  },
  {
    number: "02",
    title: "距离与方位角实时定位",
    copy: "上位机同步显示径向距离、方位角与平面位置，为三级区域判决提供输入。",
    detail: "ToF 测距 · PDoA 测角 · 卡尔曼滤波",
    video: asset("/videos/feature-2.mp4"),
    poster: asset("/videos/feature-2.jpg"),
  },
  {
    number: "03",
    title: "进入迎宾区声光提示",
    copy: "身份验证通过的钥匙进入 1–2 m 迎宾区后，系统自动触发迎宾声光。",
    detail: "身份一致 · 位置有效 · 迎宾输出",
    video: asset("/videos/feature-3.mp4"),
    poster: asset("/videos/feature-3.jpg"),
  },
  {
    number: "04",
    title: "进入开锁区自动开锁",
    copy: "系统判断钥匙进入或离开 0–1 m 开锁区，并执行开锁或恢复闭锁。",
    detail: "分区判决 · 自动开锁 · 离区复位",
    video: asset("/videos/feature-4.mp4"),
    poster: asset("/videos/feature-4.jpg"),
  },
  {
    number: "05",
    title: "修改钥匙身份 ID",
    copy: "通过上位机修改钥匙的 4 位身份 ID，并立即按新身份完成验证。",
    detail: "参数下发 · 即时生效 · 再次校验",
    video: asset("/videos/feature-5.mp4"),
    poster: asset("/videos/feature-5.jpg"),
  },
] as const;

export default function FeatureShowcase() {
  const [active, setActive] = useState(0);
  const videoRef = useRef<HTMLVideoElement>(null);
  const feature = features[active];

  useEffect(() => {
    const video = videoRef.current;
    if (!video) return;
    video.load();
    video.play().catch(() => undefined);
  }, [active]);

  return (
    <div className="feature-player">
      <div className="feature-stage">
        <video
          ref={videoRef}
          controls
          playsInline
          preload="metadata"
          poster={feature.poster}
          aria-label={feature.title}
        >
          <source src={feature.video} type="video/mp4" />
          当前浏览器不支持视频播放。
        </video>
        <div className="feature-stage-copy" aria-live="polite">
          <span>{feature.number} / 05</span>
          <div>
            <h3>{feature.title}</h3>
            <p>{feature.copy}</p>
          </div>
          <small>{feature.detail}</small>
        </div>
      </div>

      <div className="feature-selector" aria-label="选择功能演示">
        {features.map((item, index) => (
          <button
            type="button"
            className={index === active ? "active" : ""}
            aria-pressed={index === active}
            onClick={() => setActive(index)}
            key={item.number}
          >
            <img src={item.poster} alt="" loading="lazy" />
            <span>{item.number}</span>
            <strong>{item.title}</strong>
            <i aria-hidden="true">{index === active ? "播放中" : "播放"}</i>
          </button>
        ))}
      </div>
    </div>
  );
}
