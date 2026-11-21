// MG from the old game (v95), verbatim. Lines 187768-187768, 187901-187921, 187939-187939 of game.pretty.js
 const MG = MobileGPU,
 if (MG.region = function(P) {
   const [W, H] = P.full, [aw, ah] = P.alloc, s = A.on ? A.scale : 1, rw = Math.max(2, Math.min(aw, Math.round(W * s))), rh = Math.max(2, Math.min(ah, Math.round(H * s))), nb = P.cfg.bloom | 0;
   if (P.size && P.size[0] === rw && P.size[1] === rh && P._bloomN === nb) return !1;
   if (P.rt.viewport.set(0, 0, rw, rh), P.rt.scissor.set(0, 0, rw, rh), P.rt.scissorTest = rw < aw || rh < ah, disposeBloom(P), nb > 0) {
    const o2 = {
     type: T.HalfFloatType,
     format: T.RGBAFormat,
     minFilter: T.LinearFilter,
     magFilter: T.LinearFilter,
     depthBuffer: !1,
     stencilBuffer: !1,
     generateMipmaps: !1
    };
    for (let i = 0; i < nb; i++) {
     const lw = Math.max(2, rw >> i + 1),
      lh = Math.max(2, rh >> i + 1);
     P.down.push(new T.WebGLRenderTarget(lw, lh, o2)), i < nb - 1 && P.up.push(new T.WebGLRenderTarget(lw, lh, o2))
    }
   }
   return P.size = [rw, rh], P._bloomN = nb, MG.rebuilds = (MG.rebuilds || 0) + 1, !0
  }, Post.build = function() {
   this.rt = new T.WebGLRenderTarget(aw, ah, opt), this.rt.texture.name = "hdr", this.samples = samples, this.full = [W, H], this.alloc = [aw, ah], this.allocTop = top, this.down = [], this.up = [], this.size = null, this._bloomN = -1, MG.allocs = (MG.allocs || 0) + 1, MG.region(this)
