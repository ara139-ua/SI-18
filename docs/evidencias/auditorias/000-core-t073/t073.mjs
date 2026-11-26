// T073 (parte automática): Lighthouse (accesibilidad) y axe sobre InicioSesion, MainLayout y MisAvisos.
// Uso: node t073.mjs <dir-dependencias> <dir-salida>. La clave de demo llega por SI18_CLAVE_DEMO.
import { createRequire } from 'node:module';
import { writeFileSync } from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

const [deps, salida] = process.argv.slice(2);
const req = createRequire(path.join(deps, 'package.json'));
const puppeteer = req('puppeteer-core');
const { AxePuppeteer } = req('@axe-core/puppeteer');
const { startFlow } = await import(pathToFileURL(path.join(deps, 'node_modules/lighthouse/core/index.js')).href);

const BASE = 'https://localhost:7180';
const CHROME = 'C:/Program Files/Google/Chrome/Application/chrome.exe';
const CORREO = 'ciudadana.empadronada@si18.test';
const CLAVE = process.env.SI18_CLAVE_DEMO;
if (!CLAVE) throw new Error('Falta SI18_CLAVE_DEMO');

const browser = await puppeteer.launch({
  executablePath: CHROME,
  headless: true,
  acceptInsecureCerts: true,
  args: ['--ignore-certificate-errors', '--window-size=1350,940'],
  defaultViewport: null,
});
const page = await browser.newPage();

const flow = await startFlow(page, {
  name: 'T073',
  config: {
    extends: 'lighthouse:default',
    settings: {
      onlyCategories: ['accessibility'],
      disableStorageReset: true, // conservar la cookie de sesión entre pasos
      formFactor: 'desktop',
      screenEmulation: { mobile: false, width: 1350, height: 940, deviceScaleFactor: 1, disabled: false },
      throttlingMethod: 'provided',
    },
  },
});

const resultados = [];
const esperarBlazor = async (h1) => {
  await page.waitForFunction(
    (t) => [...document.querySelectorAll('h1')].some((e) => e.textContent.trim().includes(t)),
    { timeout: 60000 }, h1);
  await new Promise((r) => setTimeout(r, 800));
};
const axe = async (nombre) => {
  const r = await new AxePuppeteer(page)
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'best-practice'])
    .analyze();
  const infr = r.violations.map((v) => ({ id: v.id, impacto: v.impact, nodos: v.nodes.length, ayuda: v.help,
    objetivos: v.nodes.slice(0, 5).map((n) => n.target.join(' ')) }));
  return { estado: nombre, url: page.url(), axeVersion: r.testEngine.version, infracciones: infr,
    incompletas: r.incomplete.map((v) => ({ id: v.id, impacto: v.impact, nodos: v.nodes.length, detalle: v.nodes.map((n) => n.target.join(' ') + ' :: ' + (n.any[0]?.message ?? '')) })) };
};

// 1. InicioSesion + MainLayout sin sesión
await flow.navigate(`${BASE}/iniciar-sesion`, { name: 'InicioSesion (sin sesión)' });
await esperarBlazor('Iniciar sesión');
resultados.push(await axe('InicioSesion (sin sesión)'));

// 2. InicioSesion con mensaje de error
await page.type('input[type=email]', CORREO);
await page.type('input[type=password]', 'contrasena-incorrecta');
await page.click('button[type=submit]');
await page.waitForFunction(() => document.body.innerText.includes('Correo o contraseña incorrectos'), { timeout: 20000 });
await new Promise((r) => setTimeout(r, 800));
await flow.snapshot({ name: 'InicioSesion (con mensaje de error)' });
const estadoError = await axe('InicioSesion (con mensaje de error)');
estadoError.regionError = await page.evaluate(() => {
  const n = [...document.querySelectorAll('[aria-live],[role=alert],[role=status]')]
    .find((e) => e.textContent.includes('Correo o contraseña incorrectos'));
  if (!n) return null;
  const vivo = n.closest('[aria-live],[role=alert],[role=status]');
  return { etiqueta: vivo.tagName.toLowerCase(), role: vivo.getAttribute('role'), ariaLive: vivo.getAttribute('aria-live'),
    texto: n.textContent.trim().slice(0, 120), conIcono: !!vivo.querySelector('svg') };
});
estadoError.focoTrasError = await page.evaluate(() => {
  const a = document.activeElement; return a ? `${a.tagName.toLowerCase()}[type=${a.type}]` : null;
});
resultados.push(estadoError);

// 3. Iniciar sesión de verdad
const vaciar = async (sel) => {
  await page.focus(sel);
  await page.keyboard.down('Control'); await page.keyboard.press('KeyA'); await page.keyboard.up('Control');
  await page.keyboard.press('Backspace');
};
await vaciar('input[type=email]');
await page.type('input[type=email]', CORREO);
await vaciar('input[type=password]');
await page.type('input[type=password]', CLAVE);
await page.keyboard.press('Tab');
await new Promise((r) => setTimeout(r, 300));
await page.click('button[type=submit]');
try {
  await page.waitForFunction(() => !location.pathname.startsWith('/iniciar-sesion'), { timeout: 20000 });
} catch (e) {
  await page.screenshot({ path: path.join(salida, 'fallo-login.png') });
  console.error('Login fallido. Texto:', (await page.evaluate(() => document.body.innerText)).slice(0, 600));
  throw e;
}
await new Promise((r) => setTimeout(r, 1500));

// 4. MainLayout con sesión (página de inicio)
await flow.navigate(`${BASE}/`, { name: 'MainLayout (con sesión, /)' });
await page.waitForSelector('main', { timeout: 60000 });
await new Promise((r) => setTimeout(r, 1500));
const r4 = await axe('MainLayout (con sesión, /)');
r4.conSesion = await page.evaluate(() => document.body.textContent.includes('Cerrar sesión') && !location.pathname.startsWith('/iniciar-sesion'));
resultados.push(r4);

// 5. MisAvisos con sesión
await flow.navigate(`${BASE}/mis-avisos`, { name: 'MisAvisos (con sesión)' });
await esperarBlazor('avisos');
const r5 = await axe('MisAvisos (con sesión)');
r5.conSesion = await page.evaluate(() => document.body.textContent.includes('Cerrar sesión') && !location.pathname.startsWith('/iniciar-sesion'));
resultados.push(r5);

const informe = await flow.createFlowResult();
const lh = informe.steps.map((s) => ({
  paso: s.name, modo: s.lhr.gatherMode,
  puntuacion: s.lhr.categories.accessibility?.score == null ? null : Math.round(s.lhr.categories.accessibility.score * 100),
  fallos: Object.values(s.lhr.audits)
    .filter((a) => a.score !== null && a.score < 1 && a.scoreDisplayMode === 'binary')
    .map((a) => a.id),
}));
writeFileSync(path.join(salida, 't073-flujo.html'), await flow.generateReport());
writeFileSync(path.join(salida, 't073.json'), JSON.stringify({ lighthouse: lh, axe: resultados,
  lighthouseVersion: informe.steps[0].lhr.lighthouseVersion }, null, 2));
console.log(JSON.stringify({ lighthouse: lh, axe: resultados }, null, 2));
await browser.close();
