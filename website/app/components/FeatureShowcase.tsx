"use client";

import { useRef, useState } from "react";

const basePath = process.env.NEXT_PUBLIC_BASE_PATH ?? "";
const asset = (path: string) => `${basePath}${path}`;

const features = [
  {
    number: "01",
    title: "钥匙身份广播与识别",
    copy: "数字钥匙持续发送 4 位身份 ID，门锁在感应区完成接收、比对与状态显示。",
    detail: "钥匙信标 → UWB 基站 → 上位机验证",
    video: asset("/videos/feature-1.mp4"),
    poster: asset("/videos/feature-1.jpg"),
  },
  {
    number: "02",
    title: "距离与方位角实时定位",
    copy: "上位机同步显示径向距离、方位角与平面位置，为三级区域判决提供输入。",
    detail: "ToF 测距 → PDoA 测角 → 卡尔曼滤波",
    video: asset("/videos/feature-2.mp4"),
    poster: asset("/videos/feature-2.jpg"),
  },
  {
    number: "03",
    title: "进入迎宾区声光提示",
    copy: "身份验证通过的钥匙进入 1–2 m 迎宾区后，系统自动触发迎宾声光。",
    detail: "身份一致 → 位置有效 → 迎宾输出",
    video: asset("/videos/feature-3.mp4"),
    poster: asset("/videos/feature-3.jpg"),
  },
  {
    number: "04",
    title: "进入开锁区自动开锁",
    copy: "系统判断钥匙进入或离开 0–1 m 开锁区，并执行开锁或恢复闭锁。",
    detail: "分区判决 → 自动开锁 → 离区复位",
    video: asset("/videos/feature-4.mp4"),
    poster: asset("/videos/feature-4.jpg"),
  },
  {
    number: "05",
    title: "修改钥匙身份 ID",
    copy: "通过上位机修改钥匙的 4 位身份 ID，并立即按新身份完成验证。",
    detail: "参数下发 → 即时生效 → 再次校验",
    video: asset("/videos/feature-5.mp4"),
    poster: asset("/videos/feature-5.jpg"),
  },
] as const;

export default function FeatureShowcase() {
  const [active, setActive] = useState(0);
  const [started, setStarted] = useState(false);
  const videoRef = useRef<HTMLVideoElement>(null);
  const feature = features[active];

  const selectFeature = (index: number) => {
    if (index === active) return;
    setActive(index);
    setStarted(false);
  };

  const playVideo = () => {
    setStarted(true);
    videoRef.current?.play().catch(() => undefined);
  };

  return (
    <div className="feature-player">
      <div className="feature-selector" aria-label="选择功能演示">
        {features.map((item, index) => {
          const selected = index === active;
          return (
            <button
              type="button"
              className={selected ? "active" : ""}
              aria-pressed={selected}
              onClick={() => selectFeature(index)}
              key={item.number}
            >
              <span>{item.number}</span>
              <div>
                <strong>{item.title}</strong>
                {selected && (
                  <div className="feature-selector-detail">
                    <p>{item.copy}</p>
                    <small>{item.detail}</small>
                  </div>
                )}
              </div>
            </button>
          );
        })}
      </div>

      <div className="feature-stage" aria-live="polite">
        <div className="feature-stage-label">LIVE DEMO · {feature.number}</div>
        <div className="feature-video-shell">
          <video
            key={feature.video}
            ref={videoRef}
            controls={started}
            playsInline
            preload="metadata"
            poster={feature.poster}
            aria-label={feature.title}
            onPlay={() => setStarted(true)}
          >
            <source src={feature.video} type="video/mp4" />
            当前浏览器不支持视频播放。
          </video>
          {!started && (
            <button className="feature-play" type="button" onClick={playVideo} aria-label={`播放：${feature.title}`}>
              <span aria-hidden="true" />
            </button>
          )}
        </div>
        <div className="feature-stage-footer">
          <strong>{feature.title}</strong>
          <span>{feature.number} / {String(features.length).padStart(2, "0")}</span>
        </div>
      </div>
    </div>
  );
}
