"use client";

import { useEffect, useState } from "react";

const basePath = process.env.NEXT_PUBLIC_BASE_PATH ?? "";
const asset = (path: string) => `${basePath}${path}`;

const slides = [
  {
    image: asset("/images/software-unlock.png"),
    alt: "数字钥匙实验系统开锁区运行界面",
    title: "身份通过 · 已进入开锁区",
    fit: "cover",
  },
  {
    image: asset("/report/system-block.png"),
    alt: "数字钥匙实验系统总体框图",
    title: "系统总体构成 · UWB 定位与门锁控制",
    fit: "contain",
  },
  {
    image: asset("/report/pdoa-principle.png"),
    alt: "双天线 PDoA 方位角测量原理图",
    title: "PDoA 方位角测量 · 由相位差反演方向",
    fit: "contain",
  },
  {
    image: asset("/report/program-flow.png"),
    alt: "数字钥匙实验系统程序流程图",
    title: "身份与位置双重验证 · 状态机闭环",
    fit: "contain",
  },
  {
    image: asset("/images/team-competition.jpg"),
    alt: "谢秋实、朱拓源、庞亚宸携作品参加全国大学生电子设计竞赛",
    title: "参赛团队 · 华中科技大学",
    fit: "contain",
  },
] as const;

export default function HeroCarousel() {
  const [active, setActive] = useState(0);
  const [paused, setPaused] = useState(false);
  const slide = slides[active];

  const move = (step: number) => {
    setActive((current) => (current + step + slides.length) % slides.length);
  };

  useEffect(() => {
    if (paused || window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;
    const timer = window.setInterval(() => {
      setActive((current) => (current + 1) % slides.length);
    }, 2800);
    return () => window.clearInterval(timer);
  }, [paused]);

  return (
    <div
      className="hero-visual hero-carousel"
      role="region"
      aria-roledescription="carousel"
      aria-label="项目关键画面"
      tabIndex={0}
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
      onKeyDown={(event) => {
        if (event.key === "ArrowLeft") move(-1);
        if (event.key === "ArrowRight") move(1);
      }}
    >
      <div className="window-chrome">
        <span /><span /><span />
        <p>digital-key.console / live view · {slide.title}</p>
        <b>{String(active + 1).padStart(2, "0")} / {String(slides.length).padStart(2, "0")}</b>
        <div className="hero-carousel-dots" aria-label="选择项目画面">
          {slides.map((item, index) => (
            <button
              type="button"
              className={index === active ? "active" : ""}
              aria-label={`查看第 ${index + 1} 张：${item.title}`}
              aria-pressed={index === active}
              onClick={() => setActive(index)}
              key={item.image}
            />
          ))}
        </div>
      </div>

      <div className="hero-carousel-media" aria-live="polite">
        <img
          key={slide.image}
          src={slide.image}
          alt={slide.alt}
          data-fit={slide.fit}
        />
        <button type="button" className="hero-carousel-arrow previous" onClick={() => move(-1)} aria-label="上一张">‹</button>
        <button type="button" className="hero-carousel-arrow next" onClick={() => move(1)} aria-label="下一张">›</button>
      </div>
    </div>
  );
}
