// Puente Blazor <-> Zebra Browser Print (impresoras USB/red locales, ej. GX420d).
// Requiere en wwwroot/js: BrowserPrint-3.0.216.min.js (+ BrowserPrint-Zebra opcional)
// y la app nativa "Zebra Browser Print" instalada en el Windows del equipo.
window.scanproBrowserPrint = (function () {
  var cache = [];

  function available() {
    return typeof BrowserPrint !== "undefined";
  }

  function discover() {
    return new Promise(function (resolve) {
      if (!available()) {
        resolve({ ok: false, error: "SDK Browser Print no cargado. Copia los .js a wwwroot/js e instala la app de Zebra." });
        return;
      }
      try {
        BrowserPrint.getLocalDevices(
          function (list) {
            cache = list || [];
            // La doc recomienda pedir también el default: a veces no sale en discovery.
            try {
              BrowserPrint.getDefaultDevice("printer",
                function (dev) {
                  if (dev && !cache.some(function (d) { return d.uid === dev.uid; })) cache.push(dev);
                  done();
                },
                function () { done(); });
            } catch (e) { done(); }
            function done() {
              resolve({
                ok: true,
                devices: cache.map(function (d) {
                  return { uid: d.uid, name: d.name, connection: d.connection };
                })
              });
            }
          },
          function (err) { resolve({ ok: false, error: String(err) }); },
          "printer");
      } catch (e) {
        resolve({ ok: false, error: String((e && e.message) || e) });
      }
    });
  }

  function send(uid, zpl) {
    return new Promise(function (resolve) {
      var dev = null;
      for (var i = 0; i < cache.length; i++) {
        if (cache[i].uid === uid) { dev = cache[i]; break; }
      }
      if (!dev) {
        resolve({ ok: false, error: "Impresora no encontrada. Pulsa Buscar de nuevo." });
        return;
      }
      try {
        dev.send(zpl,
          function () { resolve({ ok: true }); },
          function (err) { resolve({ ok: false, error: String(err) }); });
      } catch (e) {
        resolve({ ok: false, error: String((e && e.message) || e) });
      }
    });
  }

  return { available: available, discover: discover, send: send };
})();
