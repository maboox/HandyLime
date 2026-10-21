// Original HandyLime artwork. Lucide paths are read from the existing licensed icon set.
// Run with Node.js: node scripts/Build-ReadmeAssets.mjs
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const root = fileURLToPath(new URL('../', import.meta.url));
const assets = path.join(root, 'docs/assets');
const font = 'Segoe UI,Arial,sans-serif';
await mkdir(assets, { recursive: true });
const esc = value => String(value).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('"', '&quot;');
const text = (x, y, value, size = 18, color = '#F2F7E9', extra = '') =>
  `<text x="${x}" y="${y}" fill="${color}" font-family="${font}" font-size="${size}" ${extra}>${esc(value)}</text>`;
const svg = (width, height, title, content) => `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" role="img" aria-labelledby="title"><title id="title">${esc(title)}</title>${content}</svg>\n`;
async function icon(name, x, y, size = 24, stroke = '#C1EC62') {
  const original = await readFile(path.join(root, 'src/HandyLime.App/Assets/Lucide', `${name}.svg`), 'utf8');
  const content = original.replace(/^[\s\S]*?<svg[^>]*>/, '').replace(/<\/svg>\s*$/, '');
  return `<g transform="translate(${x} ${y}) scale(${size / 24})" fill="none" stroke="${stroke}" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">${content}</g>`;
}
function mark(x, y, size) {
  // Geometry matches the original app mark in src/HandyLime.App/Brand.cs.
  return `<g transform="translate(${x} ${y}) scale(${size / 64})"><rect width="64" height="64" rx="17" fill="#1D310C"/><path d="M14 35C14 20 26 14 38 18C50 22 54 34 47 45C40 56 23 54 17 44Z" fill="#C1EC62"/><path d="M20 35C20 25 28 21 36 24C45 27 47 35 42 42C37 49 27 49 23 42Z" fill="#E8FFB4"/><path d="M32 35L22 30M32 35L34 24M32 35L44 35M32 35L35 46M32 35L24 43" stroke="#1D310C" stroke-width="2.8" stroke-linecap="round"/><path d="M34 16C36 7 46 6 51 10C47 18 41 21 34 16Z" fill="#C1EC62"/></g>`;
}
function sector(cx, cy, angle, selected) {
  const start = (angle - 20) * Math.PI / 180;
  const end = (angle + 20) * Math.PI / 180;
  const point = (r, a) => `${(cx + r * Math.cos(a)).toFixed(2)} ${(cy + r * Math.sin(a)).toFixed(2)}`;
  return `<path d="M${point(79,start)}L${point(176,start)}A176 176 0 0 1 ${point(176,end)}L${point(79,end)}A79 79 0 0 0 ${point(79,start)}Z" fill="${selected ? '#C1EC62' : '#273E24'}" stroke="${selected ? '#C1EC62' : '#4A6340'}" stroke-width="3" stroke-linejoin="round"/>`;
}

let hero = `<defs><linearGradient id="bg" x2="1" y2="1"><stop stop-color="#111C12"/><stop offset="1" stop-color="#233A1D"/></linearGradient><radialGradient id="glow"><stop stop-color="#B9E44D" stop-opacity=".15"/><stop offset="1" stop-color="#B9E44D" stop-opacity="0"/></radialGradient><clipPath id="clip"><rect width="1280" height="520" rx="28"/></clipPath></defs><g clip-path="url(#clip)"><rect width="1280" height="520" fill="url(#bg)"/><circle cx="1010" cy="253" r="345" fill="url(#glow)"/><g fill="none" stroke="#58783E" opacity=".25"><circle cx="978" cy="258" r="218"/><circle cx="978" cy="258" r="258"/><circle cx="978" cy="258" r="304"/></g>`;
hero += mark(58, 42, 58) + text(134, 83, 'HandyLime', 34, '#F2F7E9', 'font-weight="650"');
hero += text(62, 156, 'YOUR EVERYDAY FILE WORKSPACE', 12, '#C1EC62', 'letter-spacing="2.5" font-weight="650"');
hero += text(58, 242, 'New format.', 74, '#F2F7E9', 'font-weight="700" letter-spacing="-3"');
hero += text(58, 321, 'Same flow.', 74, '#C1EC62', 'font-weight="700" letter-spacing="-3"');
hero += text(62, 373, 'Convert. Edit. Keep moving.', 24, '#DFEACF');
hero += text(62, 408, 'A little less busywork. A lot more flow.', 18, '#B7C9A7');
const formats = ['PNG', 'WEBP', 'AVIF', 'TIFF', 'BMP', 'GIF', 'ICO', 'PDF'];
for (let i = 0; i < formats.length; i++) {
  const angle = -90 + i * 45;
  hero += sector(978, 258, angle, i === 1);
  const a = angle * Math.PI / 180;
  hero += text(978 + 126 * Math.cos(a), 264 + 126 * Math.sin(a), formats[i], 16, i === 1 ? '#19290B' : '#EAF3DE', 'text-anchor="middle" font-weight="700"');
}
hero += `<circle cx="978" cy="258" r="64" fill="#142210" stroke="#516C37"/>` + mark(938, 218, 80);
hero += await icon('mouse-pointer-2', 1102, 146, 31, '#FFFFFF');
hero += `<rect x="720" y="236" width="66" height="41" rx="9" fill="#EAF3DE"/>` + text(753, 262, 'Shift', 15, '#263B18', 'text-anchor="middle" font-weight="700"');
hero += await icon('arrow-right', 797, 244, 24, '#C1EC62');
hero += text(978, 476, 'DRAG  /  TAP A KEY  /  DROP', 12, '#BDCFAD', 'text-anchor="middle" letter-spacing="2"');
for (const [x, label, name] of [[62,'Windows x64','monitor'],[238,'Local processing','folder'],[437,'Open source','files']]) {
  hero += `<rect x="${x}" y="452" width="${label==='Local processing'?181:160}" height="36" rx="18" fill="#182916" stroke="#405734"/>`;
  hero += await icon(name, x + 13, 461, 17, '#BDDE8F');
  hero += text(x + 40, 475, label, 13, '#D8E8C6');
}
hero += '</g>';
await writeFile(path.join(assets, 'hero.svg'), svg(1280, 520, 'HandyLime — New format. Same flow. A Windows file workspace with a drag wheel.', hero));

for (const item of [
  {name:'download-portable',label:'Download Portable',detail:'.NET included · Windows x64',bg:'#C1EC62',ink:'#1D2D0D',muted:'#405922',border:'#A8CF4D',icon:'download'},
  {name:'download-lite',label:'Download Lite',detail:'.NET Desktop Runtime 10 required',bg:'#22371D',ink:'#F0F7E8',muted:'#C1D2B1',border:'#66864A',icon:'download'},
  {name:'user-guide',label:'User guide',detail:'English + Persian',bg:'#E4DCF9',ink:'#332647',muted:'#635278',border:'#C9B7EB',icon:'file-search'},
]) {
  let content = `<rect x="1" y="1" width="258" height="66" rx="14" fill="${item.bg}" stroke="${item.border}"/>`;
  content += await icon(item.icon, 18, 21, 25, item.ink);
  content += text(56, 29, item.label, 17, item.ink, 'font-weight="700"');
  content += text(56, 48, item.detail, 11, item.muted);
  await writeFile(path.join(assets, `${item.name}.svg`), svg(260, 68, `${item.label} — ${item.detail}`, content));
}

let flow = `<rect x="1" y="1" width="1198" height="244" rx="22" fill="#172412" stroke="#455B34"/>`;
const modes = [
  {x:28,key:'Shift',title:'Convert formats',detail:'PNG, WEBP, GIF and more',name:'arrow-left-right'},
  {x:425,key:'Alt',title:'Edit & adjust',detail:'Crop, resize, light and color',name:'sliders-horizontal'},
  {x:822,key:'Ctrl',title:'File actions',detail:'Copy, move, archive and organize',name:'folder-open'},
];
for (const item of modes) {
  const x = item.x;
  flow += `<rect x="${x}" y="27" width="350" height="164" rx="15" fill="#22351B" stroke="#3C542D"/>`;
  flow += `<rect x="${x+22}" y="47" width="79" height="45" rx="10" fill="#C1EC62"/>`;
  flow += text(x+61.5, 76, item.key, 19, '#20320E', 'text-anchor="middle" font-weight="700"');
  flow += await icon(item.name, x+294, 57, 25, '#C1EC62');
  flow += text(x+22, 130, item.title, 22, '#EEF6E5', 'font-weight="650"');
  flow += text(x+22, 161, item.detail, 15, '#BBCDAA');
}
flow += text(600, 222, 'Start dragging · Tap a key · Drop on an option · Tap the same key again to hide', 14, '#BDCFAB', 'text-anchor="middle"');
await writeFile(path.join(assets, 'shortcuts.svg'), svg(1200, 246, 'Drag a file, then Shift to convert, Alt to edit, or Ctrl for file actions.', flow));
console.log('Generated 5 original README SVG assets using the existing HandyLime mark and Lucide icons.');
