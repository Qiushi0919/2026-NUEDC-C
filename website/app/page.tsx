const basePath = process.env.NEXT_PUBLIC_BASE_PATH ?? "";
const asset = (path: string) => `${basePath}${path}`;

const scenarios = [
  {
    image: asset("/images/software-sensing.png"),
    eyebrow: "SENSING · 感应区",
    title: "身份通过，等待靠近",
    value: "2.60 m / −25.0°",
    tone: "cyan",
  },
  {
    image: asset("/images/software-welcome.png"),
    eyebrow: "WELCOME · 迎宾区",
    title: "迎宾声光自动开启",
    value: "1.50 m / +18.0°",
    tone: "amber",
  },
  {
    image: asset("/images/software-unlock.png"),
    eyebrow: "UNLOCK · 开锁区",
    title: "身份通过，自动开锁",
    value: "0.65 m / −8.0°",
    tone: "green",
  },
  {
    image: asset("/images/software-mismatch.png"),
    eyebrow: "DENIED · 身份不匹配",
    title: "拒绝放行，保持闭锁",
    value: "0.75 m / +10.0°",
    tone: "red",
  },
] as const;

const featureVideos = [
  {
    requirement: "要求 1",
    title: "钥匙身份广播与识别",
    copy: "数字钥匙持续发送 4 位身份 ID，门锁在感应区完成接收、识别与显示。",
    video: asset("/videos/feature-1.mp4"),
    poster: asset("/videos/feature-1.jpg"),
  },
  {
    requirement: "要求 3",
    title: "距离与方位角实时定位",
    copy: "上位机实时显示径向距离、方位角和平面位置，用于后续区域判决。",
    video: asset("/videos/feature-2.mp4"),
    poster: asset("/videos/feature-2.jpg"),
  },
  {
    requirement: "要求 4",
    title: "进入迎宾区声光提示",
    copy: "身份验证通过的钥匙进入 1–2 m 迎宾区后，系统自动给出迎宾声光提示。",
    video: asset("/videos/feature-3.mp4"),
    poster: asset("/videos/feature-3.jpg"),
  },
  {
    requirement: "要求 5",
    title: "进入开锁区自动开锁",
    copy: "系统准确判断钥匙进入或离开 0–1 m 开锁区，并执行开锁或闭锁动作。",
    video: asset("/videos/feature-4.mp4"),
    poster: asset("/videos/feature-4.jpg"),
  },
  {
    requirement: "要求 6",
    title: "修改钥匙身份 ID",
    copy: "通过上位机修改钥匙的 4 位身份 ID，并立即按修改后的身份完成识别。",
    video: asset("/videos/feature-5.mp4"),
    poster: asset("/videos/feature-5.jpg"),
  },
] as const;

const requirements = [
  ["01", "身份广播", "数字钥匙持续发送 4 位身份 ID"],
  ["02", "身份识别", "门锁读取 DIP 允许 ID 并实时比对"],
  ["03", "实时定位", "测量径向距离、方位角与平面坐标"],
  ["04", "迎宾提示", "1–2 m 内开启迎宾声光"],
  ["05", "自动开闭锁", "0–1 m 内验证通过后自动开锁"],
  ["06", "修改钥匙 ID", "上位机直接设置软件认定的 4 位身份"],
] as const;

const repo = "https://github.com/Qiushi0919/2026-NUEDC-C";

export default function Home() {
  return (
    <main>
      <header className="topbar">
        <a className="brand" href="#top" aria-label="返回首页">
          <span className="brand-mark">C</span>
          <span>
            <strong>数字钥匙实验系统</strong>
            <small>2026 NUEDC · C Problem</small>
          </span>
        </a>
        <nav aria-label="页面导航">
          <a href="#features">功能介绍</a>
          <a href="#software">软件演示</a>
          <a href="#hardware">实物展示</a>
          <a href="#system">系统方案</a>
          <a href="#resources">项目资料</a>
        </nav>
        <a className="nav-cta" href={repo} target="_blank" rel="noreferrer">
          GitHub <span>↗</span>
        </a>
      </header>

      <section className="hero" id="top">
        <div className="ambient ambient-one" />
        <div className="ambient ambient-two" />
        <div className="container hero-grid">
          <div className="hero-copy">
            <div className="hero-logos" aria-label="华中科技大学与全国大学生电子设计竞赛">
              <div className="hero-logo hero-logo-hust">
                <img src={asset("/images/hust-logo.png")} alt="华中科技大学校徽" />
              </div>
              <span className="hero-logo-divider" aria-hidden="true" />
              <div className="hero-logo hero-logo-nuedc">
                <img src={asset("/images/nuedc-badge.png")} alt="全国大学生电子设计竞赛徽章" />
              </div>
            </div>
            <div className="event-badge">
              <span className="pulse" />
              2026 全国大学生电子设计竞赛 · C 题
            </div>
            <p className="overline">WIRELESS DIGITAL KEY LAB</p>
            <h1>
              基于无线通信的
              <span>数字钥匙实验系统</span>
            </h1>
            <p className="hero-lead">
              以 120° UWB 定位基站、数字钥匙信标、Windows 上位机和蓝牙声光门锁为核心，
              完成身份验证、距离与方位测量、分区判决以及自动开闭锁闭环。
            </p>
            <div className="team-signature" aria-label="参赛队员">
              <span>华中科技大学参赛队</span>
              <strong>谢秋实 <em>队长</em></strong>
              <i aria-hidden="true">·</i>
              <strong>朱拓源</strong>
              <i aria-hidden="true">·</i>
              <strong>庞亚宸</strong>
            </div>
            <div className="hero-actions">
              <a className="button primary" href="#features">观看功能演示 <span>↓</span></a>
              <a className="button secondary" href={asset("/downloads/C题_基于无线通信的数字钥匙实验系统.pdf")} target="_blank">查看原题 PDF</a>
            </div>
            <div className="hero-facts" aria-label="系统关键指标">
              <div><strong>±45°</strong><span>有效方位角</span></div>
              <div><strong>0–3 m</strong><span>三级距离区域</span></div>
              <div><strong>4 bit</strong><span>钥匙身份 ID</span></div>
              <div><strong>115200</strong><span>蓝牙串口波特率</span></div>
            </div>
          </div>

          <div className="hero-visual">
            <div className="window-chrome">
              <span /><span /><span />
              <p>digital-key.console / live view</p>
              <b>COM22 · 10 Hz</b>
            </div>
            <img src={asset("/images/software-unlock.png")} alt="数字钥匙实验系统开锁区运行界面" />
            <div className="featured-caption">
              <span className="status-dot" />
              <p><small>FEATURED PREVIEW</small><strong>身份通过 · 已进入开锁区</strong></p>
              <b>0.65 m</b>
            </div>
          </div>
        </div>
      </section>

      <section className="section feature-videos" id="features">
        <div className="container">
          <div className="section-heading split-heading">
            <div>
              <p className="overline">FEATURE DEMONSTRATION</p>
              <h2>功能介绍视频</h2>
            </div>
            <p>五段现场演示依次对应题目要求 1、3、4、5、6。视频经过网页播放优化，点击播放即可查看完整过程。</p>
          </div>
          <div className="feature-video-grid">
            {featureVideos.map((item, index) => (
              <article className="feature-video-card" key={item.requirement}>
                <div className="feature-video-frame">
                  <video controls playsInline preload="metadata" poster={item.poster} aria-label={`${item.requirement}：${item.title}`}>
                    <source src={item.video} type="video/mp4" />
                    当前浏览器不支持视频播放。
                  </video>
                  <span>功能 {index + 1}</span>
                </div>
                <div className="feature-video-copy">
                  <small>{item.requirement}</small>
                  <h3>{item.title}</h3>
                  <p>{item.copy}</p>
                  <b>现场演示 · H.264</b>
                </div>
              </article>
            ))}
          </div>
        </div>
      </section>

      <section className="section showcase" id="software">
        <div className="container">
          <div className="section-heading split-heading">
            <div>
              <p className="overline">SOFTWARE SHOWCASE</p>
              <h2>软件运行截图</h2>
            </div>
            <p>一块屏幕完成身份、定位、区域判断、声光状态和门锁动作显示。以下四个场景由隐藏演示参数生成，正式启动不会进入演示状态。</p>
          </div>
          <div className="scenario-grid">
            {scenarios.map((item) => (
              <article className={`scenario-card ${item.tone}`} key={item.image}>
                <div className="scenario-image"><img src={item.image} alt={item.title} loading="lazy" /></div>
                <div className="scenario-copy">
                  <div><small>{item.eyebrow}</small><h3>{item.title}</h3></div>
                  <strong>{item.value}</strong>
                </div>
              </article>
            ))}
          </div>
        </div>
      </section>

      <section className="section hardware" id="hardware">
        <div className="container">
          <div className="section-heading centered">
            <p className="overline">PHYSICAL SYSTEM</p>
            <h2>实物与测试照片</h2>
            <p>数字钥匙、声光控制模块、智能门锁与现场测试装置形成完整可演示系统。</p>
          </div>
          <div className="hardware-grid">
            <figure className="hardware-card hardware-main">
              <img src={asset("/images/test-scene.jpeg")} alt="数字钥匙系统现场测试场景" loading="lazy" />
              <figcaption><span>01 · SYSTEM TEST</span><strong>完整测试场景</strong><p>基站、显示装置、门锁结构与移动供电集成测试。</p></figcaption>
            </figure>
            <figure className="hardware-card">
              <img src={asset("/images/smart-lock-1.jpeg")} alt="智能门锁整体结构一" loading="lazy" />
              <figcaption><span>02 · SMART LOCK</span><strong>智能门锁整体结构</strong></figcaption>
            </figure>
            <figure className="hardware-card">
              <img src={asset("/images/smart-lock-2.jpeg")} alt="智能门锁整体结构二" loading="lazy" />
              <figcaption><span>03 · INTEGRATION</span><strong>板卡与显示装置集成</strong></figcaption>
            </figure>
            <figure className="hardware-card">
              <img src={asset("/images/digital-key.jpg")} alt="数字钥匙信标" loading="lazy" />
              <figcaption><span>04 · DIGITAL KEY</span><strong>独立数字钥匙信标</strong></figcaption>
            </figure>
            <figure className="hardware-card">
              <img src={asset("/images/sound-light-1.jpeg")} alt="蓝牙声光控制模块" loading="lazy" />
              <figcaption><span>05 · CONTROLLER</span><strong>蓝牙声光 / DIP 模块</strong></figcaption>
            </figure>
            <figure className="hardware-card">
              <img src={asset("/images/test-map.jpg")} alt="带区域地图的完整测试场地" loading="lazy" />
              <figcaption><span>06 · TEST MAP</span><strong>120° 分区测试场地</strong><p>开锁区、迎宾区和感应区按实际距离铺设。</p></figcaption>
            </figure>
            <figure className="hardware-card">
              <img src={asset("/images/calibration-console.jpg")} alt="数字钥匙系统全局校准界面" loading="lazy" />
              <figcaption><span>07 · CALIBRATION</span><strong>全局定位校准</strong></figcaption>
            </figure>
            <figure className="hardware-card">
              <img src={asset("/images/test-integrated.jpg")} alt="数字钥匙系统整机运行现场" loading="lazy" />
              <figcaption><span>08 · LIVE SYSTEM</span><strong>整机联调运行</strong></figcaption>
            </figure>
            <figure className="hardware-card">
              <img src={asset("/images/controller-detail.jpg")} alt="数字钥匙系统控制硬件细节" loading="lazy" />
              <figcaption><span>09 · HARDWARE DETAIL</span><strong>门锁控制硬件</strong></figcaption>
            </figure>
          </div>
        </div>
      </section>

      <section className="section system" id="system">
        <div className="container">
          <div className="section-heading centered">
            <p className="overline">CLOSED-LOOP SYSTEM</p>
            <h2>从定位到门锁动作的完整闭环</h2>
            <p>定位链路与控制链路分离，Windows 上位机统一完成身份验证、区域判决和状态下发。</p>
          </div>
          <div className="pipeline" aria-label="系统数据链路">
            <div className="pipeline-node"><span>01</span><strong>数字钥匙</strong><small>持续广播身份 ID</small></div>
            <i>→</i>
            <div className="pipeline-node"><span>02</span><strong>120° UWB 基站</strong><small>距离 + 方位角</small></div>
            <i>→</i>
            <div className="pipeline-node featured"><span>03</span><strong>Windows 上位机</strong><small>身份验证 + 分区决策</small></div>
            <i>→</i>
            <div className="pipeline-node"><span>04</span><strong>蓝牙控制链路</strong><small>115200 · CRC16</small></div>
            <i>→</i>
            <div className="pipeline-node"><span>05</span><strong>智能门锁</strong><small>DIP ID + 声光动作</small></div>
          </div>

          <div className="requirements-grid">
            {requirements.map(([number, title, copy]) => (
              <article className="requirement-card" key={number}>
                <span>{number}</span><div><h3>{title}</h3><p>{copy}</p></div>
              </article>
            ))}
          </div>

          <div className="zone-panel">
            <div className="zone-copy">
              <p className="overline">RANGE DECISION</p>
              <h2>三级距离区域，状态一眼可见</h2>
              <p>有效角度为门锁正前方 ±45°。只有身份 ID 与门锁 DIP 允许 ID 一致时，才会执行迎宾和开锁动作。</p>
            </div>
            <div className="zones">
              <div className="zone unlock"><span>0–1 m</span><strong>开锁区</strong><small>验证通过 · 自动开锁</small></div>
              <div className="zone welcome"><span>1–2 m</span><strong>迎宾区</strong><small>开启声光 · 保持闭锁</small></div>
              <div className="zone sensing"><span>2–3 m</span><strong>感应区</strong><small>识别身份 · 等待靠近</small></div>
            </div>
          </div>
        </div>
      </section>

      <section className="section resources" id="resources">
        <div className="container">
          <div className="section-heading split-heading">
            <div><p className="overline">PROJECT RESOURCES</p><h2>项目资料与源码</h2></div>
            <p>题目原文、定位套件开发资料、上位机源码和声光控制固件均已归档，便于比赛现场查阅和后续复现。</p>
          </div>
          <div className="resource-grid">
            <a className="resource-card primary-resource" href={asset("/downloads/C题_基于无线通信的数字钥匙实验系统.pdf")} target="_blank">
              <span>PDF</span><div><small>COMPETITION BRIEF</small><h3>C 题原题</h3><p>基于无线通信的数字钥匙实验系统完整题面。</p></div><b>打开 ↗</b>
            </a>
            <a className="resource-card" href={`${repo}/tree/main/resources/2026年电赛原题合集`} target="_blank" rel="noreferrer">
              <span>08</span><div><small>ORIGINAL TOPICS</small><h3>2026 电赛原题合集</h3><p>A、B、C、D、E、F、G、H 题及附图。</p></div><b>浏览 ↗</b>
            </a>
            <a className="resource-card" href={`${repo}/tree/main/resources/ALX-AOA-FIT定位套件开发资料`} target="_blank" rel="noreferrer">
              <span>UWB</span><div><small>DEVELOPMENT KIT</small><h3>ALX-AOA-FIT 开发资料</h3><p>规格书、协议、示例代码、硬件文件与安装视频。</p></div><b>浏览 ↗</b>
            </a>
            <a className="resource-card" href={repo} target="_blank" rel="noreferrer">
              <span>&lt;/&gt;</span><div><small>SOURCE CODE</small><h3>完整项目仓库</h3><p>Windows 上位机、串口协议测试与 MSPM0 固件。</p></div><b>GitHub ↗</b>
            </a>
          </div>
          <div className="tech-strip">
            <span>BASE STATION <b>COM22</b></span>
            <span>CONTROL LINK <b>115200 8N1</b></span>
            <span>FRAME HEADER <b>AA 55</b></span>
            <span>FAIL-SAFE <b>500 ms</b></span>
          </div>
        </div>
      </section>

      <footer>
        <div className="container footer-grid">
          <div><strong>华中科技大学 · 2026 电赛 C 题</strong><p>谢秋实（队长） · 朱拓源 · 庞亚宸</p></div>
          <div><a href={repo} target="_blank" rel="noreferrer">项目仓库 ↗</a><a href="#top">返回顶部 ↑</a></div>
        </div>
      </footer>
    </main>
  );
}
