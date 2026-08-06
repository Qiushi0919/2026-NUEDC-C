import FeatureShowcase from "./components/FeatureShowcase";
import HardwareGallery from "./components/HardwareGallery";

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

const repo = "https://github.com/Qiushi0919/2026-NUEDC-C";
const report = asset("/downloads/2026-NUEDC-C-设计报告.pdf");

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
          <a href="#features">功能演示</a>
          <a href="#software">软件界面</a>
          <a href="#hardware">实物测试</a>
          <a href="#principles">定位原理</a>
          <a href="#design">电路程序</a>
          <a href="#results">测试结果</a>
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
              完成身份验证、距离与方位测量、分区判决以及自动开闭锁。
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
              <a className="button secondary" href={report} target="_blank">设计报告 PDF</a>
              <a className="button secondary" href={asset("/downloads/C题_基于无线通信的数字钥匙实验系统.pdf")} target="_blank">C 题原题</a>
            </div>
            <div className="hero-facts" aria-label="系统关键指标">
              <div><strong>0.04 m</strong><span>最大距离误差</span></div>
              <div><strong>2.3°</strong><span>最大角度误差</span></div>
              <div><strong>4 bit</strong><span>钥匙身份 ID</span></div>
              <div><strong>6 / 6</strong><span>功能测试正确</span></div>
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
            <p>选择右侧功能即可切换现场演示；主播放器保留完整控制条，便于暂停检查定位数据与门锁动作。</p>
          </div>
          <FeatureShowcase />
        </div>
      </section>

      <section className="section showcase" id="software">
        <div className="container">
          <div className="section-heading split-heading">
            <div>
              <p className="overline">SOFTWARE SHOWCASE</p>
              <h2>软件运行界面</h2>
            </div>
            <p>同一界面完成身份、距离、方位、平面坐标、区域状态、声光和门锁动作显示。</p>
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
            <p>首图展示 120° 分区测试场地；点击缩略图切换，点击主图可查看原始大图。</p>
          </div>
          <HardwareGallery />
        </div>
      </section>

      <section className="section principles" id="principles">
        <div className="container">
          <div className="section-heading split-heading">
            <div><p className="overline">POSITIONING PRINCIPLE</p><h2>定位原理与算法</h2></div>
            <p>系统采用单基站四天线 UWB 方案：ToF 给出径向距离，PDoA 计算方位角，再通过卡尔曼滤波稳定区域边界。</p>
          </div>

          <div className="system-overview">
            <figure className="report-figure">
              <img src={asset("/report/system-block.png")} alt="数字钥匙实验系统框图" loading="lazy" />
              <figcaption>数字钥匙通过 UWB 与门锁端通信，MSPM0G3507 统一连接定位、声光、拨码和显示模块。</figcaption>
            </figure>
            <div className="system-overview-copy">
              <span>为什么选择 UWB + PDoA</span>
              <h3>一个基站同时获得距离与方向</h3>
              <p>相较蓝牙 RSSI 和超声波方案，UWB 对多径与环境变化更不敏感；四天线阵列无需部署多台基站，适合门锁正前方 120° 范围内的实时定位。</p>
              <ul>
                <li><b>ToF 测距</b><span>由信号往返时间计算钥匙到基站的径向距离。</span></li>
                <li><b>PDoA 测角</b><span>由天线间相位差反演入射方向。</span></li>
                <li><b>区域判决</b><span>身份匹配后，按距离与方位触发迎宾或开锁。</span></li>
              </ul>
            </div>
          </div>

          <div className="algorithm-grid">
            <article className="algorithm-card formula-card">
              <span>01 · RANGE</span>
              <h3>ToF 往返时间测距</h3>
              <p>收发两端交换时间戳，扣除钥匙端应答时间后得到飞行时间，再用标定参数修正系统偏差。</p>
              <div className="formula">T<sub>tof</sub> = (T<sub>round</sub> − T<sub>reply</sub>) / 2</div>
              <div className="formula">D = k<sub>D</sub> · cT<sub>tof</sub> + b<sub>D</sub></div>
            </article>
            <article className="algorithm-card pdoa-card">
              <div>
                <span>02 · ANGLE</span>
                <h3>PDoA 相位差测角</h3>
                <p>同一平面波到达两根天线的路程差形成相位差，由天线间距 d 和波长 λ 求得方位角 α。</p>
                <div className="formula">α = arcsin(λΔφ / 2πd)</div>
              </div>
              <img src={asset("/report/pdoa-principle.png")} alt="双天线 PDoA 方位角测量示意图" loading="lazy" />
            </article>
            <article className="algorithm-card filter-card">
              <span>03 · FILTER</span>
              <h3>卡尔曼滤波与多基线融合</h3>
              <p>距离和角度分别进入一维卡尔曼滤波器；四天线提供多组基线结果，按有效性加权融合。</p>
              <ol>
                <li><b>预测</b><small>依据上一时刻状态估计当前位置</small></li>
                <li><b>校正</b><small>用新测量更新距离与角度</small></li>
                <li><b>判区</b><small>减少 1 m、2 m 边界附近的状态抖动</small></li>
              </ol>
            </article>
          </div>
        </div>
      </section>

      <section className="section engineering" id="design">
        <div className="container">
          <div className="section-heading split-heading">
            <div><p className="overline">HARDWARE &amp; SOFTWARE</p><h2>电路与程序设计</h2></div>
            <p>钥匙端使用 UWB 发射模块；门锁端以 TI MSPM0G3507 为核心，接收定位数据并驱动声光、拨码与显示模块。</p>
          </div>
          <div className="engineering-grid">
            <figure className="engineering-card engineering-wide">
              <img src={asset("/report/uwb-transmitter-circuit.png")} alt="UWB 发射模块参考电路" loading="lazy" />
              <figcaption><small>KEY NODE</small><strong>钥匙端 UWB 发射电路</strong><p>3.3 V 供电，SPI 与主控连接；EXTON、WAKE 和 RST 完成上电与复位控制。</p></figcaption>
            </figure>
            <figure className="engineering-card engineering-wide">
              <img src={asset("/report/receiver-structure.png")} alt="四天线 UWB 接收模块结构图" loading="lazy" />
              <figcaption><small>LOCK NODE</small><strong>四天线 UWB 接收结构</strong><p>接收模块输出身份 ID、距离和方位角，主控负责身份验证与区域判定。</p></figcaption>
            </figure>
            <figure className="engineering-card flow-card">
              <img src={asset("/report/program-flow.png")} alt="数字钥匙系统程序流程图" loading="lazy" />
              <figcaption><small>STATE MACHINE</small><strong>门锁端状态机</strong><p>等待 ID → 身份检测 → 读取位置 → 位置检测 → 控制声光与门锁。</p></figcaption>
            </figure>
            <article className="engineering-card protocol-card">
              <small>CONTROL LOGIC</small>
              <h3>身份与位置双重验证</h3>
              <p>门锁只在钥匙 ID 与拨码允许 ID 一致、位置数据有效且进入目标区域时执行动作。</p>
              <dl>
                <div><dt>感应区</dt><dd>2–3 m · 识别身份</dd></div>
                <div><dt>迎宾区</dt><dd>1–2 m · 开启声光</dd></div>
                <div><dt>开锁区</dt><dd>0–1 m · 自动开锁</dd></div>
                <div><dt>有效角度</dt><dd>门锁正前方 ±45°</dd></div>
              </dl>
            </article>
          </div>
        </div>
      </section>

      <section className="section results" id="results">
        <div className="container">
          <div className="section-heading centered">
            <p className="overline">VALIDATION</p>
            <h2>测试结果</h2>
            <p>六组定位精度测试与六组身份 / 区域功能测试均记录在设计报告中。</p>
          </div>
          <div className="metric-grid">
            <div><strong>0.04 <small>m</small></strong><span>最大距离误差</span></div>
            <div><strong>2.3 <small>°</small></strong><span>最大角度误差</span></div>
            <div><strong>6 / 6</strong><span>区域状态正确</span></div>
            <div><strong>100<small>%</small></strong><span>身份与动作测试正确率</span></div>
          </div>
          <div className="result-detail">
            <figure>
              <img src={asset("/report/test-results.png")} alt="定位精度与身份区域功能测试记录表" loading="lazy" />
            </figure>
            <div>
              <span>MEASURED, NOT ESTIMATED</span>
              <h3>覆盖 0.5–3.0 m 与 −40°–45°</h3>
              <p>定位测试包含近、中、远距离和左右方位；功能测试同时覆盖身份匹配、身份不匹配、开锁区、迎宾区与感应区。</p>
              <ul>
                <li>距离误差范围：0.01–0.04 m</li>
                <li>角度误差范围：0.2–2.3°</li>
                <li>不同钥匙 ID 下的预期状态与实际状态一致</li>
              </ul>
            </div>
          </div>
        </div>
      </section>

      <section className="section resources" id="resources">
        <div className="container">
          <div className="section-heading split-heading">
            <div><p className="overline">PROJECT RESOURCES</p><h2>项目资料与源码</h2></div>
            <p>设计报告、题目原文、定位套件资料和完整源码均可直接查看。</p>
          </div>
          <div className="resource-grid">
            <a className="resource-card primary-resource" href={report} target="_blank">
              <span>PDF</span><div><small>DESIGN REPORT</small><h3>作品设计报告</h3><p>定位原理、硬件电路、程序流程、系统测试与完整数据。</p></div><b>打开 ↗</b>
            </a>
            <a className="resource-card" href={asset("/downloads/C题_基于无线通信的数字钥匙实验系统.pdf")} target="_blank">
              <span>C</span><div><small>COMPETITION BRIEF</small><h3>C 题原题</h3><p>基于无线通信的数字钥匙实验系统完整题面。</p></div><b>打开 ↗</b>
            </a>
            <a className="resource-card" href={`${repo}/tree/main/resources/ALX-AOA-FIT定位套件开发资料`} target="_blank" rel="noreferrer">
              <span>UWB</span><div><small>DEVELOPMENT KIT</small><h3>ALX-AOA-FIT 开发资料</h3><p>规格书、协议、示例代码、硬件文件与安装视频。</p></div><b>浏览 ↗</b>
            </a>
            <a className="resource-card" href={repo} target="_blank" rel="noreferrer">
              <span>&lt;/&gt;</span><div><small>SOURCE CODE</small><h3>完整项目仓库</h3><p>Windows 上位机、串口协议测试与 MSPM0 固件。</p></div><b>GitHub ↗</b>
            </a>
          </div>
          <div className="tech-strip">
            <span>CORE MCU <b>MSPM0G3507</b></span>
            <span>POSITIONING <b>UWB ToF + PDoA</b></span>
            <span>CONTROL LINK <b>Bluetooth UART</b></span>
            <span>DECISION <b>ID + Range + Angle</b></span>
          </div>
        </div>
      </section>

      <footer>
        <div className="container footer-grid">
          <div><strong>华中科技大学 · 2026 电赛 C 题</strong><p>谢秋实（队长） · 朱拓源 · 庞亚宸</p></div>
          <div><a href={report} target="_blank">设计报告 ↗</a><a href={repo} target="_blank" rel="noreferrer">项目仓库 ↗</a><a href="#top">返回顶部 ↑</a></div>
        </div>
      </footer>
    </main>
  );
}
