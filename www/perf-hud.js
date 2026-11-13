/* ASBO perf HUD. Loaded only with ?hud=1 or localStorage asbo_hud=1. No dependencies (works with or without Capacitor). */
(function () {
  'use strict';
  if (window.__asboHud) return;
  window.__asboHud = true;

  var frames = [];          // frame times (ms), rolling ~5 s window for display
  var summaryFrames = [];   // frame times for the 60 s summary
  var longTasks = 0, longTasks60 = 0, longTaskMs60 = 0;
  var last = performance.now(), fpsCount = 0, fpsT0 = last, fps = 0;

  function pct(arr, p) {
    if (!arr.length) return 0;
    var s = arr.slice().sort(function (a, b) { return a - b; });
    return s[Math.min(s.length - 1, Math.floor(p * s.length))];
  }
  function heap() {
    var m = performance && performance.memory;
    return m ? (m.usedJSHeapSize / 1048576).toFixed(0) + '/' + (m.jsHeapSizeLimit / 1048576).toFixed(0) + ' MB' : 'n/a';
  }
  var cap = window.Capacitor;
  var info = {
    ua: navigator.userAgent,
    dpr: window.devicePixelRatio,
    screen: screen.width + 'x' + screen.height,
    inner: innerWidth + 'x' + innerHeight,
    cores: navigator.hardwareConcurrency || 'n/a',
    mem: navigator.deviceMemory ? navigator.deviceMemory + ' GB' : 'n/a',
    native: !!(cap && cap.isNativePlatform && cap.isNativePlatform())
  };

  try {
    if (window.PerformanceObserver) {
      new PerformanceObserver(function (list) {
        list.getEntries().forEach(function (e) { longTasks++; longTasks60++; longTaskMs60 += e.duration; });
      }).observe({ entryTypes: ['longtask'] });
    }
  } catch (e) { /* longtask not supported */ }

  var el = document.createElement('pre');
  el.style.cssText = 'position:fixed;top:4px;left:4px;z-index:2147483647;margin:0;padding:4px 6px;' +
    'font:11px/1.35 monospace;color:#9f9;background:rgba(0,0,0,.65);border-radius:4px;' +
    'pointer-events:none;white-space:pre;max-width:60vw;overflow:hidden';
  (document.body || document.documentElement).appendChild(el);

  function tick(now) {
    var dt = now - last; last = now;
    frames.push(dt); summaryFrames.push(dt);
    if (frames.length > 300) frames.shift();
    fpsCount++;
    if (now - fpsT0 >= 500) { fps = fpsCount * 1000 / (now - fpsT0); fpsCount = 0; fpsT0 = now; render(); }
    requestAnimationFrame(tick);
  }
  function render() {
    el.textContent =
      'FPS ' + fps.toFixed(0) + '  frame p50 ' + pct(frames, 0.5).toFixed(1) + ' / p95 ' + pct(frames, 0.95).toFixed(1) + ' ms\n' +
      'long tasks ' + longTasks + '  heap ' + heap() + '\n' +
      'dpr ' + info.dpr + '  scr ' + info.screen + '  view ' + info.inner + '  cores ' + info.cores + '\n' +
      (info.native ? 'native wrapper' : 'browser') + '\n' + info.ua.slice(0, 90);
  }
  function summarize() {
    var n = summaryFrames.length;
    var avg = n ? summaryFrames.reduce(function (a, b) { return a + b; }, 0) / n : 0;
    var s = {
      seconds: 60, frames: n, avgFps: avg ? +(1000 / avg).toFixed(1) : 0,
      p50ms: +pct(summaryFrames, 0.5).toFixed(1), p95ms: +pct(summaryFrames, 0.95).toFixed(1),
      p99ms: +pct(summaryFrames, 0.99).toFixed(1), worstMs: +Math.max.apply(null, summaryFrames.concat([0])).toFixed(1),
      longTasks: longTasks60, longTaskMs: +longTaskMs60.toFixed(0), heap: heap(), device: info
    };
    console.log('[asbo-hud] last 60s', JSON.stringify(s));
    (window.__asboHudLog = window.__asboHudLog || []).push(s);
    summaryFrames = []; longTasks60 = 0; longTaskMs60 = 0;
  }
  setInterval(summarize, 60000);
  console.log('[asbo-hud] started', JSON.stringify(info));
  requestAnimationFrame(function (t) { last = t; fpsT0 = t; requestAnimationFrame(tick); });
})();
