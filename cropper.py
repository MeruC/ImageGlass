import tkinter as tk
from tkinter import filedialog
from PIL import Image, ImageTk
import os
from datetime import datetime


ZOOM_STEP    = 1.15
ZOOM_MIN     = 0.05
ZOOM_MAX     = 64.0
PROXY_MAX_PX = 2000


class CropperApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Quick Cropper")
        self.root.configure(bg="#1e1e1e")
        self.root.bind("<Return>",    self.save_crop)
        self.root.bind("<Escape>",    self.clear_selection)
        self.root.bind("<Control-o>", lambda e: self.load_image())
        self.root.bind("<Control-0>", lambda e: self.zoom_fit())
        self.root.bind("<l>",         lambda e: self.toggle_lock())
        self.root.bind("<x>",         lambda e: self.toggle_axis("x"))
        self.root.bind("<y>",         lambda e: self.toggle_axis("y"))

        self.image_orig  = None
        self.image_proxy = None
        self.proxy_ratio = 1.0
        self.photo       = None
        self.source_path = None
        self.save_dir    = None

        self.img_x = 0.0
        self.img_y = 0.0
        self.scale = 1.0

        self._render_pending = False
        self._hq_timer       = None

        # Selection in proxy-image pixel coords (x1,y1,x2,y2)
        self.sel     = None
        self.rect_id = None

        # Locked size in proxy px (w, h)  — None means free-draw
        self.locked_size = None

        # What the current left-button drag is doing: "draw" | "move"
        self._drag_mode      = "draw"
        self._sel_start_orig = (0.0, 0.0)   # anchor for draw
        self._move_offset    = (0.0, 0.0)   # cursor offset into sel for move
        self._locked_axis    = None          # "x", "y", or None

        self._pan_sx = 0;   self._pan_sy = 0
        self._pan_ix = 0.0; self._pan_iy = 0.0

        self._build_ui()

    # ------------------------------------------------------------------ UI

    def _build_ui(self):
        bar = tk.Frame(self.root, bg="#2d2d2d", pady=4)
        bar.pack(fill=tk.X, side=tk.TOP)

        def btn(parent, text, cmd, color="#3c3c3c", hover="#505050"):
            b = tk.Button(parent, text=text, command=cmd,
                          bg=color, fg="white", relief=tk.FLAT,
                          padx=10, pady=4, cursor="hand2",
                          activebackground=hover, activeforeground="white")
            b.pack(side=tk.LEFT, padx=4)
            return b

        btn(bar, "Open Image  (Ctrl+O)", self.load_image)
        btn(bar, "Set Save Folder",      self.choose_save_dir)
        btn(bar, "Save Crop  (Enter)",   self.save_crop, "#0d6efd", "#0b5ed7")

        tk.Label(bar, text="  Zoom:", bg="#2d2d2d", fg="#aaaaaa").pack(side=tk.LEFT)
        btn(bar, "−", lambda: self.zoom_by(1 / ZOOM_STEP))
        btn(bar, "+", lambda: self.zoom_by(ZOOM_STEP))
        btn(bar, "Fit (Ctrl+0)", self.zoom_fit)

        self.zoom_label = tk.Label(bar, text="100%", bg="#2d2d2d", fg="#aaaaaa", width=6)
        self.zoom_label.pack(side=tk.LEFT, padx=2)

        tk.Frame(bar, bg="#444", width=1, padx=0).pack(side=tk.LEFT, fill=tk.Y, pady=4, padx=6)

        self.lock_btn = tk.Button(bar, text="Lock Size  (L)", command=self.toggle_lock,
                                  bg="#3c3c3c", fg="#aaaaaa", relief=tk.FLAT,
                                  padx=10, pady=4, cursor="hand2",
                                  activebackground="#505050", activeforeground="white")
        self.lock_btn.pack(side=tk.LEFT, padx=4)

        self.lock_label = tk.Label(bar, text="", bg="#2d2d2d", fg="#f0a030", width=18, anchor="w")
        self.lock_label.pack(side=tk.LEFT, padx=2)

        tk.Frame(bar, bg="#444", width=1).pack(side=tk.LEFT, fill=tk.Y, pady=4, padx=6)
        tk.Label(bar, text="Axis:", bg="#2d2d2d", fg="#aaaaaa").pack(side=tk.LEFT)
        self.axis_x_btn = tk.Button(bar, text="X  (x)", command=lambda: self.toggle_axis("x"),
                                    bg="#3c3c3c", fg="#aaaaaa", relief=tk.FLAT,
                                    padx=8, pady=4, cursor="hand2",
                                    activebackground="#505050", activeforeground="white")
        self.axis_x_btn.pack(side=tk.LEFT, padx=2)
        self.axis_y_btn = tk.Button(bar, text="Y  (y)", command=lambda: self.toggle_axis("y"),
                                    bg="#3c3c3c", fg="#aaaaaa", relief=tk.FLAT,
                                    padx=8, pady=4, cursor="hand2",
                                    activebackground="#505050", activeforeground="white")
        self.axis_y_btn.pack(side=tk.LEFT, padx=2)

        self.status = tk.Label(bar, text="Open an image to get started",
                               bg="#2d2d2d", fg="#aaaaaa", anchor="w")
        self.status.pack(side=tk.LEFT, padx=12, fill=tk.X, expand=True)

        self.save_label = tk.Label(bar, text="Save: (same as source)",
                                   bg="#2d2d2d", fg="#666666")
        self.save_label.pack(side=tk.RIGHT, padx=12)

        frame = tk.Frame(self.root, bg="#1e1e1e")
        frame.pack(fill=tk.BOTH, expand=True)

        self.canvas = tk.Canvas(frame, bg="#2a2a2a", cursor="crosshair",
                                highlightthickness=0)
        self.canvas.pack(fill=tk.BOTH, expand=True)

        self.canvas.bind("<Configure>",       self._on_resize)
        self.canvas.bind("<ButtonPress-1>",   self._sel_press)
        self.canvas.bind("<B1-Motion>",       self._sel_drag)
        self.canvas.bind("<ButtonRelease-1>", self._sel_release)
        self.canvas.bind("<ButtonPress-2>",   self._pan_press)
        self.canvas.bind("<B2-Motion>",       self._pan_drag)
        self.canvas.bind("<ButtonPress-3>",   self._pan_press)
        self.canvas.bind("<B3-Motion>",       self._pan_drag)
        self.canvas.bind("<MouseWheel>",      self._on_wheel)
        self.canvas.bind("<Motion>",          self._on_hover)

    # --------------------------------------------------------------- axis lock

    def toggle_axis(self, axis):
        if self._locked_axis == axis:
            self._locked_axis = None
            self.axis_x_btn.config(bg="#3c3c3c", fg="#aaaaaa")
            self.axis_y_btn.config(bg="#3c3c3c", fg="#aaaaaa")
        else:
            self._locked_axis = axis
            self.axis_x_btn.config(bg="#3c3c3c" if axis != "x" else "#005f5f",
                                   fg="#aaaaaa"  if axis != "x" else "white")
            self.axis_y_btn.config(bg="#3c3c3c" if axis != "y" else "#005f5f",
                                   fg="#aaaaaa"  if axis != "y" else "white")

    # --------------------------------------------------------------- lock size

    def toggle_lock(self, _=None):
        if self.locked_size is not None:
            # unlock
            self.locked_size = None
            self.lock_btn.config(bg="#3c3c3c", fg="#aaaaaa")
            self.lock_label.config(text="")
        else:
            # lock to current selection
            if self.sel is None:
                self.status.config(text="Draw a selection first, then lock.")
                return
            x1, y1, x2, y2 = self.sel
            w = abs(x2 - x1)
            h = abs(y2 - y1)
            if w < 2 or h < 2:
                self.status.config(text="Selection too small to lock.")
                return
            self.locked_size = (w, h)
            # show size in original px
            ow = int(w * self.proxy_ratio)
            oh = int(h * self.proxy_ratio)
            ar = _aspect_ratio_str(ow, oh)
            self.lock_btn.config(bg="#a06000", fg="white")
            self.lock_label.config(text=f"{ow}×{oh}  ({ar})")
            self.status.config(text=f"Size locked: {ow}×{oh} — drag to reposition")

    # --------------------------------------------------------------- loading

    def load_image(self, _=None):
        path = filedialog.askopenfilename(
            title="Open Image",
            filetypes=[("Images", "*.png *.jpg *.jpeg *.webp *.bmp *.tiff *.gif"),
                       ("All files", "*.*")])
        if not path:
            return
        self.source_path = path
        img = Image.open(path)
        img.load()
        if img.mode not in ("RGB", "RGBA"):
            img = img.convert("RGBA")
        self.image_orig = img

        pw, ph = img.size
        longest = max(pw, ph)
        if longest > PROXY_MAX_PX:
            ratio = PROXY_MAX_PX / longest
            self.image_proxy = img.resize(
                (max(1, int(pw * ratio)), max(1, int(ph * ratio))), Image.LANCZOS)
            self.proxy_ratio = 1.0 / ratio
        else:
            self.image_proxy = img
            self.proxy_ratio = 1.0

        self.clear_selection()

        self.root.update_idletasks()
        self.zoom_fit()

        name = os.path.basename(path)
        lock_hint = "  · L to lock size" if self.locked_size is None else "  · size locked"
        self.status.config(
            text=f"{name}  |  {img.width}×{img.height}  |  "
                 f"Drag to select · Scroll to zoom · Right-drag to pan · Enter to save{lock_hint}")
        if self.save_dir is None:
            self.save_label.config(text=f"Save: {os.path.dirname(path)}")

    # --------------------------------------------------------------- zoom/pan

    def zoom_fit(self, _=None):
        if self.image_proxy is None:
            return
        self.root.update_idletasks()
        cw = self.canvas.winfo_width()
        ch = self.canvas.winfo_height()
        pw, ph = self.image_proxy.size
        self.scale = min(cw / pw, ch / ph)
        self.img_x = (cw - pw * self.scale) / 2
        self.img_y = (ch - ph * self.scale) / 2
        self._render(hq=True)

    def zoom_by(self, factor, cx=None, cy=None):
        if self.image_proxy is None:
            return
        new_scale = max(ZOOM_MIN, min(ZOOM_MAX, self.scale * factor))
        if new_scale == self.scale:
            return
        if cx is None: cx = self.canvas.winfo_width()  / 2
        if cy is None: cy = self.canvas.winfo_height() / 2
        ox = (cx - self.img_x) / self.scale
        oy = (cy - self.img_y) / self.scale
        self.scale = new_scale
        self.img_x = cx - ox * self.scale
        self.img_y = cy - oy * self.scale
        self._request_render()

    def _on_wheel(self, event):
        if self.image_proxy is None:
            return
        if event.state & 0x4:  # Ctrl held — pan vertically
            step = -event.delta * 0.5
            self.img_y -= step
            self._move_items()
        else:
            factor = 1 / ZOOM_STEP if event.delta < 0 else ZOOM_STEP
            self.zoom_by(factor, event.x, event.y)

    def _pan_press(self, event):
        self._pan_sx = event.x;  self._pan_sy = event.y
        self._pan_ix = self.img_x; self._pan_iy = self.img_y

    def _pan_drag(self, event):
        self.img_x = self._pan_ix + (event.x - self._pan_sx)
        self.img_y = self._pan_iy + (event.y - self._pan_sy)
        self._move_items()

    def _on_resize(self, *_):
        if self.image_proxy is not None:
            self._render(hq=True)

    # --------------------------------------------------------------- render

    def _request_render(self):
        if self._render_pending:
            return
        self._render_pending = True
        self.root.after_idle(self._do_pending_render)
        if self._hq_timer:
            self.root.after_cancel(self._hq_timer)
        self._hq_timer = self.root.after(200, lambda: self._render(hq=True))

    def _do_pending_render(self):
        self._render_pending = False
        self._render(hq=False)

    def _render(self, hq=True):
        """Render only the visible viewport crop — output is always ~canvas-sized."""
        if self.image_proxy is None:
            return
        cw = max(1, self.canvas.winfo_width())
        ch = max(1, self.canvas.winfo_height())
        pw = float(self.image_proxy.size[0])
        ph = float(self.image_proxy.size[1])

        # Visible region in proxy coords
        px0 = max(0.0, (-self.img_x) / self.scale)
        py0 = max(0.0, (-self.img_y) / self.scale)
        px1 = min(pw,  (cw - self.img_x) / self.scale)
        py1 = min(ph,  (ch - self.img_y) / self.scale)
        if px1 <= px0 or py1 <= py0:
            return

        # Canvas-space dimensions of the visible crop
        tdw = max(1, int((px1 - px0) * self.scale))
        tdh = max(1, int((py1 - py0) * self.scale))
        place_x = max(0, int(self.img_x))
        place_y = max(0, int(self.img_y))

        if hq:
            r = self.proxy_ratio
            ow, oh = self.image_orig.size
            ox0 = max(0,  int(px0 * r));  oy0 = max(0,  int(py0 * r))
            ox1 = min(ow, int(px1 * r));  oy1 = min(oh, int(py1 * r))
            src = self.image_orig.crop((ox0, oy0, ox1, oy1))
            # nearest-neighbour when zoomed in past 100% — crisp pixels, no blur
            filt = Image.NEAREST if self.scale / self.proxy_ratio >= 1.0 else Image.LANCZOS
            resized = src.resize((tdw, tdh), filt)
        else:
            ix0, iy0 = int(px0), int(py0)
            ix1, iy1 = min(int(self.image_proxy.size[0]), int(px1)), \
                       min(int(self.image_proxy.size[1]), int(py1))
            src = self.image_proxy.crop((ix0, iy0, ix1, iy1))
            resized = src.resize((tdw, tdh), Image.NEAREST)

        photo = ImageTk.PhotoImage(resized)
        self.photo = photo

        if self.canvas.find_withtag("image"):
            self.canvas.itemconfigure("image", image=photo)
            self.canvas.coords("image", place_x, place_y)
        else:
            self.canvas.create_image(place_x, place_y,
                                     anchor=tk.NW, image=photo, tags="image")

        self._sync_sel()
        self.zoom_label.config(text=f"{int(self.scale * 100)}%")

    def _move_items(self):
        """Pan: render the new viewport crop at low quality, then schedule HQ."""
        self._render(hq=False)
        if self._hq_timer:
            self.root.after_cancel(self._hq_timer)
        self._hq_timer = self.root.after(150, lambda: self._render(hq=True))

    # --------------------------------------------------------------- selection

    HANDLE_HIT = 8   # canvas-pixel hit radius for edge/corner handles

    _HANDLE_CURSORS = {
        "nw": "top_left_corner",  "n": "top_side",          "ne": "top_right_corner",
        "e":  "right_side",       "se": "bottom_right_corner","s": "bottom_side",
        "sw": "bottom_left_corner","w": "left_side",
        "move": "fleur",          None: "crosshair",
    }

    def _sync_sel(self):
        if self.sel is None or self.rect_id is None:
            return
        cx1, cy1 = self._p2c(self.sel[0], self.sel[1])
        cx2, cy2 = self._p2c(self.sel[2], self.sel[3])
        self.canvas.coords(self.rect_id, cx1, cy1, cx2, cy2)

    def _get_handle(self, cx, cy):
        """Return which handle (or 'move' / None) a canvas point hits."""
        if self.sel is None:
            return None
        lx, ty = self._p2c(self.sel[0], self.sel[1])
        rx, by = self._p2c(self.sel[2], self.sel[3])
        lx, rx = min(lx, rx), max(lx, rx)
        ty, by = min(ty, by), max(ty, by)
        H = self.HANDLE_HIT
        if not (lx - H <= cx <= rx + H and ty - H <= cy <= by + H):
            return None
        on_l = abs(cx - lx) <= H
        on_r = abs(cx - rx) <= H
        on_t = abs(cy - ty) <= H
        on_b = abs(cy - by) <= H
        if on_t and on_l: return "nw"
        if on_t and on_r: return "ne"
        if on_b and on_l: return "sw"
        if on_b and on_r: return "se"
        if on_t: return "n"
        if on_b: return "s"
        if on_l: return "w"
        if on_r: return "e"
        if lx <= cx <= rx and ty <= cy <= by:
            return "move"
        return None

    def _on_hover(self, event):
        if self.image_proxy is None:
            return
        handle = self._get_handle(event.x, event.y)
        self.canvas.config(cursor=self._HANDLE_CURSORS.get(handle, "crosshair"))

    def _sel_press(self, event):
        if self.image_proxy is None:
            return
        handle = self._get_handle(event.x, event.y)
        px, py = self._c2p(event.x, event.y)

        if handle == "move":
            self._drag_mode   = "move"
            x1, y1, *_        = self.sel
            self._move_offset = (px - x1, py - y1)
        elif handle in ("nw","n","ne","e","se","s","sw","w"):
            self._drag_mode    = handle   # e.g. "resize-nw" encoded as the handle name
        else:
            # draw new selection
            self._drag_mode      = "draw"
            self._sel_start_orig = (px, py)
            self.sel = (px, py, px, py)
            if self.rect_id:
                self.canvas.delete(self.rect_id)
            self.rect_id = self.canvas.create_rectangle(
                event.x, event.y, event.x, event.y,
                outline="#00d4ff", width=2, dash=(5, 3), tags="sel")

    def _sel_drag(self, event):
        if self.image_proxy is None:
            return
        px, py = self._c2p(event.x, event.y)
        pw, ph = float(self.image_proxy.size[0]), float(self.image_proxy.size[1])
        px = max(0.0, min(pw, px))
        py = max(0.0, min(ph, py))

        if self._drag_mode == "move":
            ox, oy = self._move_offset
            x1, y1, x2, y2 = self.sel
            w = x2 - x1;  h = y2 - y1
            if self._locked_axis == "x":
                py = y1 + oy          # keep Y unchanged
            elif self._locked_axis == "y":
                px = x1 + ox          # keep X unchanged
            nx1 = max(0.0, min(pw - w, px - ox))
            ny1 = max(0.0, min(ph - h, py - oy))
            self.sel = (nx1, ny1, nx1 + w, ny1 + h)

        elif self._drag_mode == "draw":
            if self.locked_size is not None:
                lw, lh = self.locked_size
                sx, sy = self._sel_start_orig
                self.sel = (sx, sy,
                             sx + lw * (1.0 if px >= sx else -1.0),
                             sy + lh * (1.0 if py >= sy else -1.0))
            else:
                self.sel = (*self._sel_start_orig, px, py)

        else:  # resize handle
            x1, y1, x2, y2 = self.sel
            h = self._drag_mode
            if "n" in h: y1 = min(py, y2 - 1)
            if "s" in h: y2 = max(py, y1 + 1)
            if "w" in h: x1 = min(px, x2 - 1)
            if "e" in h: x2 = max(px, x1 + 1)
            self.sel = (x1, y1, x2, y2)

        self._sync_sel()
        x1, y1, x2, y2 = self.sel
        w = abs(int((x2 - x1) * self.proxy_ratio))
        h = abs(int((y2 - y1) * self.proxy_ratio))
        self.status.config(text=f"Selection: {w}×{h} px  |  Enter to save")

    def _sel_release(self, event):
        if self.image_proxy is None:
            return
        px, py = self._c2p(event.x, event.y)
        if self._drag_mode == "draw":
            if self.locked_size is not None:
                lw, lh = self.locked_size
                sx, sy = self._sel_start_orig
                self.sel = (sx, sy,
                             sx + lw * (1.0 if px >= sx else -1.0),
                             sy + lh * (1.0 if py >= sy else -1.0))
            else:
                self.sel = (*self._sel_start_orig, px, py)
            self._sync_sel()
        # normalize so x1<x2, y1<y2 (makes edge detection stable)
        if self.sel is not None:
            x1, y1, x2, y2 = self.sel
            self.sel = (min(x1,x2), min(y1,y2), max(x1,x2), max(y1,y2))

    def clear_selection(self, _=None):
        self.sel = None
        if self.rect_id:
            self.canvas.delete(self.rect_id)
            self.rect_id = None

    # ------------------------------------------------- coordinate helpers

    def _c2p(self, cx, cy):
        pw, ph = self.image_proxy.size
        return (max(0.0, min(float(pw), (cx - self.img_x) / self.scale)),
                max(0.0, min(float(ph), (cy - self.img_y) / self.scale)))

    def _p2c(self, px, py):
        return px * self.scale + self.img_x, py * self.scale + self.img_y

    # --------------------------------------------------------------- saving

    def choose_save_dir(self):
        d = filedialog.askdirectory(title="Choose save folder")
        if d:
            self.save_dir = d
            self.save_label.config(text=f"Save: {d}")

    def save_crop(self, _=None):
        if self.image_orig is None:
            self.status.config(text="No image loaded.")
            return
        if self.sel is None:
            self.status.config(text="Draw a selection first.")
            return

        px1, py1, px2, py2 = self.sel
        r = self.proxy_ratio
        left   = int(min(px1, px2) * r)
        top    = int(min(py1, py2) * r)
        right  = int(max(px1, px2) * r)
        bottom = int(max(py1, py2) * r)

        ow, oh = self.image_orig.size
        left, top     = max(0, left),  max(0, top)
        right, bottom = min(ow, right), min(oh, bottom)

        if right - left < 2 or bottom - top < 2:
            self.status.config(text="Selection too small — drag a bigger area.")
            return

        cropped = self.image_orig.crop((left, top, right, bottom))

        out_dir = self.save_dir or os.path.dirname(self.source_path)
        src_ext = os.path.splitext(self.source_path)[1].lower()
        ext = src_ext if src_ext in (".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tiff") else ".png"

        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S_%f")[:19]
        filename  = f"crop_{timestamp}{ext}"
        out_path  = os.path.join(out_dir, filename)

        save_kwargs = {}
        if ext in (".jpg", ".jpeg"):
            save_kwargs = {"quality": 100, "subsampling": 0}
        elif ext == ".webp":
            save_kwargs = {"quality": 100, "method": 6}

        if ext in (".jpg", ".jpeg") and cropped.mode == "RGBA":
            bg = Image.new("RGB", cropped.size, (255, 255, 255))
            bg.paste(cropped, mask=cropped.split()[3])
            cropped = bg

        cropped.save(out_path, **save_kwargs)
        w, h = cropped.size
        self.status.config(
            text=f"Saved  {filename}  ({w}×{h})  — select next area and hit Enter")


# ------------------------------------------------------------------ helpers

def _gcd(a, b):
    while b:
        a, b = b, a % b
    return a

def _aspect_ratio_str(w, h):
    if w <= 0 or h <= 0:
        return "?"
    g = _gcd(w, h)
    return f"{w//g}:{h//g}"


def main():
    root = tk.Tk()
    root.geometry("1200x800")
    root.minsize(600, 400)
    CropperApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()
