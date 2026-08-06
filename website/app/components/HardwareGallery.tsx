"use client";

import { useEffect, useState } from "react";

const basePath = process.env.NEXT_PUBLIC_BASE_PATH ?? "";
const asset = (path: string) => `${basePath}${path}`;

const photos = [
  {
    image: asset("/images/test-map.jpg"),
    title: "120° 分区测试场地",
    copy: "以门锁为圆心标定开锁区、迎宾区与感应区，用于验证距离和方位判决。",
  },
  {
    image: asset("/images/smart-lock-1.jpeg"),
    title: "智能门锁整体结构",
    copy: "机械门锁、显示装置与控制硬件组成可独立运行的执行端。",
  },
  {
    image: asset("/images/smart-lock-2.jpeg"),
    title: "板卡与显示装置集成",
    copy: "主控、通信、声光控制和状态显示集中安装在门锁端。",
  },
  {
    image: asset("/images/digital-key.jpg"),
    title: "独立数字钥匙信标",
    copy: "钥匙端持续发送身份信息与定位信号，支持软件修改 4 位 ID。",
  },
  {
    image: asset("/images/sound-light-1.jpeg"),
    title: "蓝牙声光 / DIP 模块",
    copy: "DIP 开关设定允许身份，蓝牙链路接收上位机的声光与门锁动作。",
  },
  {
    image: asset("/images/test-scene.jpeg"),
    title: "完整测试场景",
    copy: "基站、显示装置、门锁结构与移动供电在现场完成整机联调。",
  },
  {
    image: asset("/images/calibration-console.jpg"),
    title: "全局定位校准",
    copy: "通过标定参数修正距离与角度系统误差，稳定区域边界。",
  },
  {
    image: asset("/images/test-integrated.jpg"),
    title: "整机联调运行",
    copy: "软件、定位基站与门锁控制端同时运行，验证端到端动作。",
  },
  {
    image: asset("/images/controller-detail.jpg"),
    title: "门锁控制硬件",
    copy: "MSPM0 主控、接口与驱动电路完成身份判决后的本地执行。",
  },
] as const;

export default function HardwareGallery() {
  const [active, setActive] = useState(0);
  const [open, setOpen] = useState(false);
  const photo = photos[active];

  useEffect(() => {
    if (!open) return;
    const close = (event: KeyboardEvent) => {
      if (event.key === "Escape") setOpen(false);
    };
    window.addEventListener("keydown", close);
    return () => window.removeEventListener("keydown", close);
  }, [open]);

  return (
    <>
      <div className="hardware-gallery">
        <button className="hardware-stage" type="button" onClick={() => setOpen(true)} aria-label={`放大查看：${photo.title}`}>
          <img src={photo.image} alt={photo.title} />
          <span>点击查看大图 ↗</span>
          <div>
            <small>{String(active + 1).padStart(2, "0")} / {String(photos.length).padStart(2, "0")}</small>
            <h3>{photo.title}</h3>
            <p>{photo.copy}</p>
          </div>
        </button>

        <div className="hardware-thumbnails" aria-label="实物与测试照片">
          {photos.map((item, index) => (
            <button
              type="button"
              className={index === active ? "active" : ""}
              aria-pressed={index === active}
              onClick={() => setActive(index)}
              key={item.image}
            >
              <img src={item.image} alt="" loading="lazy" />
              <span>{String(index + 1).padStart(2, "0")}</span>
              <strong>{item.title}</strong>
            </button>
          ))}
        </div>
      </div>

      {open && (
        <div className="lightbox" role="dialog" aria-modal="true" aria-label={photo.title} onClick={() => setOpen(false)}>
          <button type="button" onClick={() => setOpen(false)} aria-label="关闭大图">×</button>
          <figure onClick={(event) => event.stopPropagation()}>
            <img src={photo.image} alt={photo.title} />
            <figcaption><strong>{photo.title}</strong><span>{photo.copy}</span></figcaption>
          </figure>
        </div>
      )}
    </>
  );
}
