window.resizeHelper = {
   getWindowHeight: () => window.innerHeight,

   getElementHeight: (id) => {
      const el = document.getElementById(id);
      return el ? el.offsetHeight : 0;
   },

   registerResizeCallback: function (dotnetHelper, elementId) {
      const notify = () => {
         const height = window.innerHeight;
         const headerHeight = document.getElementById(elementId)?.offsetHeight || 0;
         dotnetHelper.invokeMethodAsync("OnResize", height, headerHeight);
      };

      // 初回通知（DOM安定後）
      setTimeout(notify, 100);

      // ウィンドウリサイズ対応
      window.addEventListener("resize", notify);

      // ヘッダーのサイズ変更監視
      const headerEl = document.getElementById(elementId);
      if (headerEl && typeof ResizeObserver !== "undefined") {
         const observer = new ResizeObserver(() => notify());
         observer.observe(headerEl);
      }
   }
};
