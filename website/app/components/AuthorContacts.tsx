"use client";

import { useRef } from "react";

export default function AuthorContacts() {
  const dialog = useRef<HTMLDialogElement>(null);
  return (
    <>
      <div className="author-contacts" role="group" aria-label="谢秋实的联系方式">
        <a className="contact-icon" aria-label="邮箱：qiushi1152@gmail.com" data-label="邮箱：qiushi1152@gmail.com" href="mailto:qiushi1152@gmail.com"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 3h18a3 3 0 0 1 3 3v1L12 15 0 7V6a3 3 0 0 1 3-3Zm-3 7 12 8 12-8v8a3 3 0 0 1-3 3H3a3 3 0 0 1-3-3Z"/></svg></a>
        <a className="contact-icon" aria-label="GitHub：Qiushi0919" data-label="GitHub：Qiushi0919" href="https://github.com/Qiushi0919" target="_blank" rel="noopener noreferrer"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 .3a12 12 0 0 0-3.8 23.4c.6.1.8-.3.8-.6v-2.2c-3.3.7-4-1.4-4-1.4-.6-1.4-1.4-1.8-1.4-1.8-1.1-.8.1-.8.1-.8 1.2.1 1.8 1.2 1.8 1.2 1.1 1.8 2.9 1.3 3.6 1 .1-.8.4-1.3.8-1.6-2.7-.3-5.5-1.4-5.5-6a4.7 4.7 0 0 1 1.2-3.3c-.1-.3-.5-1.6.2-3.3 0 0 1-.3 3.3 1.2a11.4 11.4 0 0 1 6 0c2.3-1.5 3.3-1.2 3.3-1.2.7 1.7.3 3 .2 3.3a4.7 4.7 0 0 1 1.2 3.3c0 4.6-2.8 5.7-5.5 6 .4.4.8 1.1.8 2.2v3.3c0 .3.2.7.8.6A12 12 0 0 0 12 .3Z"/></svg></a>
        <button className="contact-icon" aria-label="微信：查看谢秋实的二维码" data-label="微信：查看谢秋实的二维码" type="button" aria-haspopup="dialog" onClick={() => dialog.current?.showModal()}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M9 2C4 2 0 5.3 0 9.3c0 2.3 1.3 4.4 3.5 5.8L2.7 18l3.5-1.7c.9.3 1.8.4 2.8.4-.4-.9-.6-1.9-.6-2.9 0-4 3.8-7.3 8.5-7.3h.9C16.5 3.8 13 2 9 2Zm-3.2 4.3a1.2 1.2 0 1 1 0 2.4 1.2 1.2 0 0 1 0-2.4Zm6.1 0a1.2 1.2 0 1 1 0 2.4 1.2 1.2 0 0 1 0-2.4Z"/><path d="M17 8c-3.9 0-7 2.6-7 5.8s3.1 5.8 7 5.8c.8 0 1.6-.1 2.3-.3L22 21l-.6-2.5c1.6-1.1 2.6-2.8 2.6-4.7C24 10.6 20.9 8 17 8Zm-2.5 3.3a1 1 0 1 1 0 2 1 1 0 0 1 0-2Zm5 0a1 1 0 1 1 0 2 1 1 0 0 1 0-2Z"/></svg></button>
        <a className="contact-icon" aria-label="个人网站：谢秋实的科研与作品" data-label="个人网站：谢秋实的科研与作品" href="https://qiushi0919.cn/" target="_blank" rel="noopener noreferrer"><svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="10" fill="none" stroke="currentColor" strokeWidth="1.8"/><ellipse cx="12" cy="12" rx="4.5" ry="10" fill="none" stroke="currentColor" strokeWidth="1.8"/><path d="M2 12h20M4 6.5h16M4 17.5h16" fill="none" stroke="currentColor" strokeWidth="1.6"/></svg></a>
        <a className="contact-icon" aria-label="谷歌学术：检索谢秋实的论文" data-label="谷歌学术：检索谢秋实的论文" href="https://scholar.google.com/scholar?q=%22Qiushi+Xie%22+OR+%22%E8%B0%A2%E7%A7%8B%E5%AE%9E%22" target="_blank" rel="noopener noreferrer"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="m12 2 12 7-12 7L0 9Zm-7 11 7 4 7-4v5c-4 3-10 3-14 0Zm17-1 2-1v9h-2Z"/></svg></a>
      </div>
      <dialog ref={dialog} className="contact-dialog" aria-labelledby="author-wechat-title" onKeyDown={(event) => {
        if (event.key === "Escape") dialog.current?.close();
      }} onClick={(event) => {
        if (event.target !== event.currentTarget) return;
        const bounds = event.currentTarget.getBoundingClientRect();
        if (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom) dialog.current?.close();
      }}>
        <h2 id="author-wechat-title">谢秋实的微信</h2>
        <p>使用微信扫描二维码</p>
        <img src="https://qiushi0919.cn/assets/contact/wechat-personal.jpg" alt="谢秋实的个人微信二维码" loading="lazy" />
        <form method="dialog"><button className="contact-dialog-close">关闭</button></form>
      </dialog>
    </>
  );
}
