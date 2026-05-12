"""
Generate ui_fx_death_corner_glow.png — soft red corner glow PNG used by
DeathPopupBuilder at the 4 screen corners (rotated 0/90/180/270 per corner).

Design:
  - 256x256 transparent PNG
  - Soft red radial glow biased to the TOP-LEFT corner of the PNG
  - Brightest at (0,0), fully transparent past radius
  - Gauss-blurred for "neon warning" diffusion
"""
import os
from PIL import Image, ImageFilter

SIZE = 256
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..",
                   "Assets", "Sprites", "Wheel", "ui_fx_death_corner_glow.png")

img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
px = img.load()
# radius defines how far the glow reaches into the PNG; ~0.85 of the side
radius = SIZE * 0.85
# falloff exponent — higher = sharper hot center, softer outer ring
gamma = 2.0
# inner color (corner) and outer cutoff
inner = (255, 30, 30)

for y in range(SIZE):
    for x in range(SIZE):
        # distance from top-left corner of the PNG
        d = (x * x + y * y) ** 0.5
        if d > radius:
            continue
        t = d / radius
        # cosine-like soft falloff for natural neon diffusion
        a = (1.0 - t) ** gamma
        alpha = int(255 * a)
        if alpha <= 0:
            continue
        px[x, y] = (inner[0], inner[1], inner[2], alpha)

# slight gaussian blur to remove any banding from the per-pixel falloff
img = img.filter(ImageFilter.GaussianBlur(radius=4))
img.save(OUT)
print("wrote", OUT, img.size)
