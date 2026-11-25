namespace WebCanvasDotNet.Core;

/// <summary>
/// The CSS shared by the design canvas and the generated static site. Both use
/// the same <c>wc-site</c> root class, so what you see in Design is the same
/// stylesheet the published pages receive.
/// </summary>
public static class SiteBlocks
{
    public const string Css = """
/* WebCanvas site blocks. Every value comes from the project theme variables. */

.wc-site {
    background-color: var(--site-bg, #f7f4ee);
    color: var(--site-text, #1d2a22);
    font-family: var(--site-font-body, "Segoe UI", sans-serif);
    font-size: 15px;
    line-height: 1.65;
    -webkit-font-smoothing: antialiased;
}

.wc-site * {
    box-sizing: border-box;
}

.wc-site p {
    margin: 0 0 16px;
}

.wc-site h1,
.wc-site h2,
.wc-site h3,
.wc-site h4,
.wc-site h5,
.wc-site h6 {
    margin: 0;
    font-family: var(--site-font-heading, Georgia, serif);
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--site-text);
}

.wc-site a {
    color: inherit;
    text-decoration: none;
}

.wc-site img {
    max-width: 100%;
}

.wc-site .site-shell {
    width: 100%;
    max-width: 1140px;
    margin: 0 auto;
    padding: 0 36px;
}

.wc-site .site-header {
    width: 100%;
    border-bottom: 1px solid var(--site-border);
    background-color: var(--site-bg);
}

.wc-site .site-header-actions {
    display: flex;
    align-items: center;
    gap: 14px;
    flex-shrink: 0;
}

.wc-site .site-header .site-btn {
    height: 36px;
    padding: 0 15px;
    font-size: 12px;
}

.wc-site .header-split .site-brand {
    flex-shrink: 0;
}

.wc-site .header-split .site-nav {
    margin: 0 auto;
}

.wc-site .header-centered-inner {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 12px;
    padding-top: 20px;
    padding-bottom: 16px;
}

.wc-site .header-centered .site-nav {
    justify-content: center;
}

.wc-site .site-header-inner {
    display: flex;
    align-items: center;
    justify-content: space-between;
    height: 76px;
    min-height: 76px;
    gap: 32px;
}

.wc-site .site-header-inner > * {
    align-self: center;
}

.wc-site .site-brand {
    display: flex;
    align-items: center;
    gap: 10px;
    font-family: var(--site-font-heading);
    font-size: 21px;
    letter-spacing: -0.01em;
}

.wc-site .site-mark {
    width: 26px;
    height: 26px;
    border-radius: 8px;
    background-color: var(--site-accent);
    box-shadow: inset 0 0 0 6px rgba(255, 255, 255, 0.22);
}

.wc-site .site-nav {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 26px;
    font-size: 13px;
    color: var(--site-muted);
}

.wc-site .site-nav a:hover {
    color: var(--site-text);
}

.wc-site .site-nav a.is-active {
    color: var(--site-accent);
}

.wc-site .site-nav .site-btn {
    color: #ffffff;
}

.wc-site .eyebrow {
    display: block;
    margin-bottom: 18px;
    font-size: 11px;
    font-weight: 600;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--site-accent);
}

.wc-site .site-hero {
    padding-top: 88px;
    padding-bottom: 64px;
}

.wc-site .hero-grid {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 56px;
}

.wc-site .hero-main {
    flex: 1 1 460px;
    min-width: 0;
}

.wc-site .hero-title {
    font-size: 62px;
    line-height: 1.02;
    letter-spacing: -0.035em;
    margin-bottom: 24px;
}

.wc-site .hero-copy {
    max-width: 530px;
    margin-bottom: 30px;
    font-size: 17px;
    line-height: 1.7;
    color: var(--site-muted);
}

.wc-site .hero-actions {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 12px;
    margin-bottom: 34px;
}

.wc-site .trust-row {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 24px;
    font-size: 12px;
    color: var(--site-muted);
}

.wc-site .trust-row span {
    display: flex;
    align-items: center;
    gap: 7px;
}

.wc-site .trust-dot {
    width: 5px;
    height: 5px;
    border-radius: 50%;
    background-color: var(--site-accent);
}

.wc-site .hero-art {
    flex: 1 1 330px;
    min-width: 0;
    padding: 18px;
    border: 1px solid var(--site-border);
    border-radius: calc(var(--site-radius) + 6px);
    background-color: var(--site-surface);
    box-shadow: 0 24px 60px rgba(31, 42, 34, 0.10);
}

.wc-site .art-frame {
    display: flex;
    flex-direction: column;
    gap: 10px;
    padding: 16px;
    border-radius: var(--site-radius);
    background-color: var(--site-bg);
}

.wc-site .art-bar {
    display: flex;
    align-items: center;
    gap: 5px;
    margin-bottom: 6px;
}

.wc-site .art-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background-color: var(--site-border);
}

.wc-site .art-head {
    height: 12px;
    width: 42%;
    border-radius: 4px;
    background-color: var(--site-text);
    opacity: 0.85;
}

.wc-site .art-line {
    height: 7px;
    border-radius: 4px;
    background-color: var(--site-border);
}

.wc-site .art-line.is-short {
    width: 62%;
}

.wc-site .art-block {
    height: 122px;
    margin-top: 8px;
    border-radius: 8px;
    background-image: linear-gradient(135deg, var(--site-accent), rgba(31, 42, 34, 0.55));
    opacity: 0.92;
}

.wc-site .art-cards {
    display: flex;
    gap: 8px;
    margin-top: 8px;
}

.wc-site .art-card {
    flex: 1;
    height: 54px;
    border-radius: 7px;
    border: 1px solid var(--site-border);
    background-color: var(--site-surface);
}

.wc-site .site-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    height: 44px;
    padding: 0 20px;
    border: 1px solid var(--site-accent);
    border-radius: calc(var(--site-radius) - 2px);
    background-color: var(--site-accent);
    color: #ffffff;
    font-family: var(--site-font-body);
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
}

.wc-site .site-btn.ghost {
    background-color: transparent;
    color: var(--site-text);
    border-color: var(--site-border);
}

.wc-site .site-btn.small {
    height: 34px;
    padding: 0 14px;
    font-size: 12px;
}

.wc-site .site-section {
    padding-top: 68px;
    padding-bottom: 68px;
}

.wc-site .site-section.is-tight {
    padding-top: 40px;
    padding-bottom: 40px;
}

.wc-site .section-head {
    display: flex;
    align-items: flex-end;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 24px;
    margin-bottom: 34px;
}

.wc-site .section-title {
    font-size: 38px;
    line-height: 1.14;
    letter-spacing: -0.025em;
}

.wc-site .section-lead {
    max-width: 460px;
    font-size: 14px;
    color: var(--site-muted);
}

.wc-site .section-kicker {
    display: block;
    margin-bottom: 10px;
    font-size: 11px;
    font-weight: 600;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--site-muted);
}

.wc-site .feature-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 18px;
}

.wc-site .feature {
    flex: 1 1 260px;
    min-width: 0;
    padding: 26px;
    border: 1px solid var(--site-border);
    border-radius: var(--site-radius);
    background-color: var(--site-surface);
}

.wc-site .feature-icon {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 38px;
    height: 38px;
    margin-bottom: 18px;
    border-radius: 10px;
    background-color: var(--site-accent);
    color: #ffffff;
    font-size: 14px;
    opacity: 0.95;
}

.wc-site .feature-title {
    margin-bottom: 8px;
    font-size: 19px;
    letter-spacing: -0.01em;
}

.wc-site .feature-copy {
    margin: 0;
    font-size: 13.5px;
    line-height: 1.65;
    color: var(--site-muted);
}

.wc-site .collection-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 22px;
}

.wc-site .post-card {
    display: block;
    flex: 1 1 280px;
    min-width: 0;
    border: 1px solid var(--site-border);
    border-radius: var(--site-radius);
    background-color: var(--site-surface);
    overflow: hidden;
}

.wc-site a.post-card {
    color: inherit;
    text-decoration: none;
    transition: border-color 160ms ease, transform 160ms ease;
}

.wc-site a.post-card:hover {
    border-color: var(--site-accent);
    transform: translateY(-2px);
}

.wc-site .article-body {
    display: flex;
    flex-direction: column;
    gap: 20px;
    max-width: 760px;
    font-size: 16px;
    line-height: 1.8;
    color: var(--site-text);
}

.wc-site .article-body p {
    margin: 0;
    white-space: pre-wrap;
}

.wc-site .post-thumb {
    height: 172px;
    background-image: linear-gradient(135deg, rgba(47, 107, 79, 0.92), rgba(247, 244, 238, 0.35));
}

.wc-site .post-thumb.is-warm {
    background-image: linear-gradient(135deg, rgba(160, 98, 58, 0.92), rgba(247, 244, 238, 0.35));
}

.wc-site .post-thumb.is-slate {
    background-image: linear-gradient(135deg, rgba(54, 71, 92, 0.92), rgba(247, 244, 238, 0.35));
}

.wc-site .post-body {
    padding: 22px;
}

.wc-site .post-meta {
    margin-bottom: 10px;
    font-size: 10.5px;
    font-weight: 600;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: var(--site-muted);
}

.wc-site .post-title {
    margin-bottom: 9px;
    font-size: 20px;
    line-height: 1.3;
    letter-spacing: -0.015em;
}

.wc-site .post-excerpt {
    margin: 0;
    font-size: 13.5px;
    line-height: 1.6;
    color: var(--site-muted);
}

.wc-site .site-band {
    padding: 56px 0;
    background-color: var(--site-text);
    color: var(--site-surface);
}

.wc-site .site-band .section-title,
.wc-site .site-band h2,
.wc-site .site-band h3 {
    color: var(--site-surface);
}

.wc-site .site-band .site-btn {
    background-color: var(--site-surface);
    border-color: var(--site-surface);
    color: var(--site-text);
}

.wc-site .site-band-inner {
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 28px;
}

.wc-site .site-band-copy {
    max-width: 600px;
}

.wc-site .site-band .section-lead {
    color: var(--site-surface);
    opacity: 0.72;
}

.wc-site .site-footer {
    width: 100%;
    padding: 48px 0 40px;
    border-top: 1px solid var(--site-border);
    font-size: 13px;
    color: var(--site-muted);
}

.wc-site .footer-nav {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 22px;
    font-size: 13px;
    color: var(--site-muted);
}

.wc-site .footer-nav a:hover {
    color: var(--site-text);
}

.wc-site .footer-col .footer-nav {
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
}

.wc-site .footer-simple-inner {
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 24px;
}

.wc-site .footer-simple .site-brand,
.wc-site .footer-newsletter .site-brand,
.wc-site .footer-centered .site-brand {
    margin: 0;
}

.wc-site .footer-newsletter-inner {
    display: flex;
    align-items: flex-end;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 32px;
}

.wc-site .footer-newsletter-copy {
    max-width: 560px;
}

.wc-site .footer-newsletter-copy p {
    margin: 8px 0 0;
}

.wc-site .footer-centered {
    text-align: center;
}

.wc-site .footer-centered .site-brand {
    justify-content: center;
    margin: 0 auto;
}

.wc-site .footer-centered .footer-nav {
    justify-content: center;
    margin-top: 16px;
}

.wc-site .footer-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 40px;
}

.wc-site .footer-col {
    flex: 1 1 200px;
}

.wc-site .footer-brand {
    font-family: var(--site-font-heading);
    font-size: 18px;
    color: var(--site-text);
    margin-bottom: 10px;
}

.wc-site .footer-links {
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.wc-site .footer-links a:hover {
    color: var(--site-text);
}

.wc-site .footer-fine {
    margin-top: 34px;
    padding-top: 20px;
    border-top: 1px solid var(--site-border);
    font-size: 11.5px;
}

.wc-site .page-head {
    padding-top: 72px;
    padding-bottom: 44px;
    border-bottom: 1px solid var(--site-border);
}

.wc-site .page-title {
    font-size: 54px;
    line-height: 1.05;
    letter-spacing: -0.035em;
    margin-bottom: 18px;
}

.wc-site .page-lead {
    max-width: 620px;
    font-size: 17px;
    line-height: 1.7;
    color: var(--site-muted);
}

.wc-site .about-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 56px;
}

.wc-site .about-copy {
    flex: 1 1 480px;
}

.wc-site .about-side {
    flex: 1 1 260px;
}

.wc-site .stat-row {
    display: flex;
    flex-wrap: wrap;
    gap: 34px;
    margin-top: 28px;
}

.wc-site .stat strong {
    display: block;
    font-family: var(--site-font-heading);
    font-size: 36px;
    font-weight: 500;
    line-height: 1.1;
    letter-spacing: -0.03em;
}

.wc-site .stat span {
    font-size: 12px;
    color: var(--site-muted);
}

.wc-site .quote {
    margin: 0;
    padding: 30px 34px;
    border-left: 3px solid var(--site-accent);
    background-color: var(--site-surface);
    border-radius: 0 var(--site-radius) var(--site-radius) 0;
    font-family: var(--site-font-heading);
    font-size: 26px;
    line-height: 1.4;
    letter-spacing: -0.02em;
}

.wc-site .quote cite {
    display: block;
    margin-top: 16px;
    font-family: var(--site-font-body);
    font-size: 12px;
    font-style: normal;
    color: var(--site-muted);
}

.wc-site .team-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 18px;
}

.wc-site .team-card {
    display: flex;
    align-items: center;
    flex: 1 1 300px;
    gap: 16px;
    padding: 20px;
    border: 1px solid var(--site-border);
    border-radius: var(--site-radius);
    background-color: var(--site-surface);
}

.wc-site .avatar {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 52px;
    height: 52px;
    flex-shrink: 0;
    border-radius: 50%;
    background-color: var(--site-accent);
    color: #ffffff;
    font-family: var(--site-font-heading);
    font-size: 20px;
}

.wc-site .team-name {
    font-size: 15px;
    font-weight: 600;
}

.wc-site .team-role {
    font-size: 12px;
    color: var(--site-muted);
}

.wc-site .contact-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 56px;
}

.wc-site .contact-form {
    flex: 1 1 440px;
    display: flex;
    flex-direction: column;
    gap: 16px;
}

.wc-site .contact-side {
    flex: 1 1 260px;
}

.wc-site .field {
    display: flex;
    flex-direction: column;
    gap: 7px;
}

.wc-site .field label {
    font-size: 11px;
    font-weight: 600;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--site-muted);
}

.wc-site .field input,
.wc-site .field textarea {
    padding: 12px 14px;
    border: 1px solid var(--site-border);
    border-radius: 8px;
    background-color: var(--site-surface);
    color: var(--site-text);
    font-family: var(--site-font-body);
    font-size: 14px;
}

.wc-site .detail-list {
    display: flex;
    flex-direction: column;
    gap: 20px;
}

.wc-site .detail-label {
    font-size: 10.5px;
    font-weight: 600;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: var(--site-muted);
    margin-bottom: 4px;
}

.wc-site .detail-value {
    font-size: 15px;
}

.wc-site .prose {
    max-width: 720px;
    font-size: 16px;
    line-height: 1.75;
}

.wc-site .prose p {
    margin-bottom: 20px;
}

.wc-site .prose h2 {
    margin: 36px 0 14px;
    font-size: 30px;
}

.wc-site .prose h3 {
    margin: 28px 0 10px;
    font-size: 22px;
}

.wc-site .chips {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
}

.wc-site .chip {
    padding: 5px 11px;
    border: 1px solid var(--site-border);
    border-radius: 20px;
    font-size: 11.5px;
    color: var(--site-muted);
    background-color: var(--site-surface);
}

.wc-site .muted {
    color: var(--site-muted);
}

.wc-site .accent {
    color: var(--site-accent);
}

.wc-site .text-link {
    color: var(--site-accent);
    text-decoration: underline;
    text-underline-offset: 3px;
}

.wc-site .text-link:hover {
    color: var(--site-text);
}

.wc-site .code-block {
    margin: 0;
    padding: 24px 26px;
    overflow: auto;
    border: 1px solid var(--site-border);
    border-radius: var(--site-radius);
    background-color: #1d232b;
    color: #e7edf3;
    font-family: Consolas, "Cascadia Code", monospace;
    font-size: 13px;
    line-height: 1.65;
    white-space: pre-wrap;
    tab-size: 4;
}

.wc-site .embed-video {
    display: block;
    width: 100%;
    max-width: 100%;
    aspect-ratio: 16 / 9;
    border: 0;
    border-radius: var(--site-radius);
    background-color: #101418;
}

.wc-site .embed-frame {
    display: block;
    width: 100%;
    min-height: 440px;
    border: 1px solid var(--site-border);
    border-radius: var(--site-radius);
    background-color: var(--site-surface);
}

.wc-canvas .wc-embed {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 9px;
    width: 100%;
    min-height: 260px;
    aspect-ratio: auto;
    padding: 34px;
    border: 1px dashed var(--site-border);
    border-radius: var(--site-radius);
    background-image: linear-gradient(135deg, rgba(47, 107, 79, 0.10), rgba(247, 244, 238, 0.25));
    color: var(--site-muted);
    text-align: center;
}

.wc-canvas .embed-video.wc-embed {
    aspect-ratio: 16 / 9;
    min-height: 0;
    padding: 0;
    color: #d8dee7;
}

.wc-canvas .embed-video.wc-embed .wc-embed-title {
    color: #ffffff;
}

.wc-canvas .embed-video.wc-embed .wc-embed-source {
    color: #97a1ad;
}

.wc-canvas .embed-frame.wc-embed {
    min-height: 440px;
    aspect-ratio: auto;
}

.wc-embed-icon {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 52px;
    height: 52px;
    border-radius: 50%;
    background-color: var(--site-accent);
    color: #ffffff;
    font-size: 18px;
    line-height: 1;
}

.wc-embed-title {
    font-family: var(--site-font-heading);
    font-size: 18px;
    color: var(--site-text);
}

.wc-embed-source {
    max-width: 84%;
    overflow: hidden;
    color: var(--site-muted);
    font-size: 11px;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.wc-site .wc-media {
    display: flex;
    align-items: center;
    justify-content: center;
    min-height: 180px;
    border: 1px dashed var(--site-border);
    border-radius: var(--site-radius);
    background-image: linear-gradient(135deg, rgba(47, 107, 79, 0.12), rgba(247, 244, 238, 0.2));
    color: var(--site-muted);
    font-size: 12px;
}

.wc-site .spacer {
    height: 32px;
}

.wc-site .divider {
    height: 1px;
    background-color: var(--site-border);
    margin: 8px 0;
}

/* ------------------------------------------------ composable starter blocks */

.wc-site .grid {
    display: grid;
    gap: 20px;
}

.wc-site .grid-2 {
    grid-template-columns: repeat(2, minmax(0, 1fr));
}

.wc-site .grid-3 {
    grid-template-columns: repeat(3, minmax(0, 1fr));
}

.wc-site .grid-4 {
    grid-template-columns: repeat(4, minmax(0, 1fr));
}

.wc-site .grid-card {
    padding: 24px;
    border: 1px solid var(--site-border);
    border-radius: var(--site-radius);
    background-color: var(--site-surface);
}

.wc-site .full-width {
    width: 100%;
}

.wc-site .full-width-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 28px;
    padding: 28px 36px;
}

.wc-site .full-width-copy {
    flex: 1 1 360px;
    min-width: 0;
}

.wc-site .full-width-row .section-lead {
    margin: 8px 0 0;
}

.wc-site .full-width-row .site-btn {
    flex-shrink: 0;
    align-self: center;
}

.wc-site .grid > .full-width,
.wc-site .feature-grid > .full-width,
.wc-site .collection-grid > .full-width,
.wc-site .team-grid > .full-width {
    grid-column: 1 / -1;
}

.wc-site .hero-grid > .full-width {
    flex: 0 0 100%;
}

.wc-site .subtitle {
    font-size: 24px;
    line-height: 1.25;
    letter-spacing: -0.02em;
}

.wc-site .bullet-list {
    margin: 0 0 18px;
    padding-left: 20px;
    color: var(--site-muted);
}

.wc-site .bullet-list li {
    margin: 6px 0;
}

.wc-site .button-row {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 12px;
}

.wc-site .media-split {
    display: grid;
    grid-template-columns: minmax(0, 1.05fr) minmax(0, 0.95fr);
    gap: 52px;
    align-items: center;
    padding: 44px 36px;
}

.wc-site .media-split.is-reverse {
    grid-template-columns: minmax(0, 0.95fr) minmax(0, 1.05fr);
}

.wc-site .media-split.is-reverse .media-frame {
    order: 2;
}

.wc-site .media-frame {
    margin: 0;
    min-height: 280px;
    overflow: hidden;
    border: 1px solid var(--site-border);
    border-radius: calc(var(--site-radius) + 4px);
    background-color: var(--site-surface);
}

.wc-site .media-frame img,
.wc-site .media-card img {
    display: block;
    width: 100%;
    height: 100%;
    object-fit: cover;
}

.wc-site .media-frame .wc-media,
.wc-site .media-card .wc-media {
    min-height: 280px;
    height: 100%;
    border: 0;
    border-radius: 0;
}

.wc-site .media-copy .section-title {
    margin-bottom: 14px;
}

.wc-site .media-copy .section-lead {
    margin-bottom: 22px;
}

.wc-site .media-card {
    overflow: hidden;
    border: 1px solid var(--site-border);
    border-radius: var(--site-radius);
    background-color: var(--site-surface);
}

.wc-site .media-card .wc-media {
    min-height: 220px;
}

.wc-site .media-card-body {
    padding: 22px;
}

.wc-site .media-card-body .feature-title {
    margin-bottom: 8px;
}

.wc-site .logo-row {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 18px 42px;
    padding: 26px 0;
    color: var(--site-muted);
}

.wc-site .logo-word {
    font-family: var(--site-font-heading);
    font-size: 20px;
    letter-spacing: -0.02em;
    opacity: 0.72;
}

.wc-site .faq-list {
    display: grid;
    gap: 18px;
}

.wc-site .faq-item {
    padding: 22px 24px;
    border: 1px solid var(--site-border);
    border-radius: var(--site-radius);
    background-color: var(--site-surface);
}

.wc-site .faq-item .feature-title {
    margin-bottom: 8px;
}

.wc-canvas {
    position: relative;
    background-color: #ffffff;
    transition: width 140ms ease;
}

.wc-canvas .wc-node {
    outline: 1px solid transparent;
    outline-offset: -1px;
}

.wc-canvas.is-editing .wc-node {
    cursor: pointer;
}

.wc-canvas.is-editing .wc-node:hover {
    outline: 1px dashed var(--wc-hover);
    outline-offset: -1px;
}

.wc-canvas.is-editing .wc-node.is-selected {
    outline: 2px solid var(--wc-select);
    outline-offset: -2px;
}

.wc-canvas.is-editing .wc-node.is-drop-inside {
    outline: 2px dashed var(--wc-select);
    outline-offset: -3px;
    background-color: rgba(45, 91, 209, 0.06);
}

.wc-canvas.is-dragging,
.wc-canvas.is-dragging .wc-node {
    cursor: copy;
}

.wc-empty-canvas {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 6px;
    min-height: 360px;
    margin: 36px;
    padding: 40px;
    border: 2px dashed var(--site-border);
    border-radius: calc(var(--site-radius) + 4px);
    background-color: var(--site-surface);
    color: var(--site-muted);
    text-align: center;
}

.wc-empty-icon {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 34px;
    height: 34px;
    margin-bottom: 4px;
    border-radius: 50%;
    background-color: var(--site-accent);
    color: #ffffff;
    font-size: 18px;
    line-height: 1;
}

.wc-empty-title {
    font-family: var(--site-font-heading);
    font-size: 22px;
    color: var(--site-text);
}

.wc-empty-copy {
    max-width: 360px;
    font-size: 13px;
}

.wc-block-wrap {
    position: relative;
}

.wc-block-label {
    position: absolute;
    left: 6px;
    top: 6px;
    z-index: 3;
    padding: 2px 7px;
    border-radius: 8px;
    background-color: var(--site-accent);
    color: #ffffff;
    font-family: "Segoe UI", sans-serif;
    font-size: 9px;
    letter-spacing: 0.02em;
    pointer-events: none;
    opacity: 0;
    transition: opacity 120ms ease;
}

.wc-block-wrap:hover .wc-block-label {
    opacity: 0.95;
}

.wc-block-missing {
    padding: 18px;
    border: 1px dashed var(--site-border, #d5d9e0);
    border-radius: 6px;
    color: var(--site-muted, #65707f);
    font-family: "Segoe UI", sans-serif;
    font-size: 12px;
}
""";
}
