"""Concept preview of the procedural slime: re-implements SlimeVisual.Surface() and the limb arch
(quadratic Bezier through a raised knee, tapered rings, bone tip) in numpy for a static picture.
Not a Unity render; lighting and materials are approximate."""
import sys
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d.art3d import Poly3DCollection

def norm(v):
    v = np.asarray(v, float); return v / np.linalg.norm(v)

HUMP, LOBE, KNOT, SAG = norm([-.42, .78, -.46]), norm([.62, .22, .5]), norm([-.18, .42, .88]), norm([.7, -.3, -.62])

def bump(d, axis, width):
    t = np.clip((d @ axis - (1 - width)) / width, 0, 1)
    return t * t * (3 - 2 * t)

def surface(d):  # unity axes: x right, y up, z forward
    r = .5 * (1 + .42 * bump(d, HUMP, .5) + .24 * bump(d, LOBE, .45) + .14 * bump(d, KNOT, .3)
              + .18 * bump(d, SAG, .55) + .05 * np.sin(d[..., 0] * 7 + d[..., 2] * 4) * np.cos(d[..., 1] * 5))
    p = np.stack([d[..., 0] * r * 1.7, d[..., 1] * r, d[..., 2] * r * 1.42], -1)
    low = p[..., 1] < -.17
    spread = 1 + (-.17 - p[..., 1]) * .9
    p[..., 0] = np.where(low, p[..., 0] * spread, p[..., 0])
    p[..., 2] = np.where(low, p[..., 2] * spread, p[..., 2])
    p[..., 1] = np.where(low, -.17 - (-.17 - p[..., 1]) * .25, p[..., 1])
    return p * 1.3

def body_mesh(n=40):
    th, ph = np.linspace(0, np.pi, n), np.linspace(0, 2 * np.pi, 2 * n)
    T, P = np.meshgrid(th, ph, indexing="ij")
    d = np.stack([np.sin(T) * np.cos(P), np.cos(T), np.sin(T) * np.sin(P)], -1)
    return surface(d) + np.array([0, .36, 0])

def limb(root, anchor, length, radius, e=1.0, grasp=False, pull=0.0, rings=14, sides=7):
    root, anchor = np.asarray(root, float), np.asarray(anchor, float)
    end = root + (anchor - root) * e
    dist = np.linalg.norm(end - root)
    reach = dist + .3 if grasp else length * e
    half = max(reach, dist) * .5
    lift = np.sqrt(max(0, half * half - dist * dist * .25)) * (.35 if grasp else .85) + .12 * e
    knee = (root + end) * .5 + np.array([0, lift, 0])
    ctrl = knee * 2 - (root + end) * .5
    thick = radius * min(1, e * 3) * (1 + .35 * pull)
    rings_pts, tips = [], []
    for i in range(rings):
        u = i / rings; a = 1 - u
        c = a * a * root + 2 * a * u * ctrl + u * u * end
        t = norm(2 * a * (ctrl - root) + 2 * u * (end - ctrl))
        right = np.cross(t, [0, 1, 0]); right = norm(right if np.linalg.norm(right) > 1e-3 else np.cross(t, [1, 0, 0]))
        up = np.cross(right, t)
        r = max(thick * (1.15 - .95 * u + .18 * np.sin(u * np.pi) - ((u - .8) * 1.6 if u > .8 else 0)), thick * .08)
        ang = np.linspace(0, 2 * np.pi, sides + 1)
        rings_pts.append(np.array([c + (right * np.cos(g) + up * np.sin(g)) * r for g in ang]))
    point = end + norm(end - ctrl) * (thick * 2.6 + .08)
    return rings_pts, point

LAYOUT = [(-34, 3.2, .2), (28, 3.55, .22), (-82, 2.65, .17), (76, 2.95, .19), (-124, 2.4, .16), (118, 2.75, .17), (-158, 2.1, .14)]

def pose(mode):
    limbs = []
    for i, (deg, length, rad) in enumerate(LAYOUT):
        a = np.radians(deg)
        root = np.array([np.sin(a) * .8, .44 + .08 * np.cos(a * 3), np.cos(a) * .65])
        foot = np.array([np.sin(a), 0, np.cos(a)]) * length * .66
        if mode == "walk":
            # group 0 planted ahead/behind, group 1 mid-step (lifted, partly retracted)
            e = 1.0 if i in (0, 3, 4) else (.55 if i in (1, 5) else 1.0)
            foot = foot + np.array([0, .04, (.5 if i in (0, 3) else -.35)])
        elif mode == "strike":
            e, foot = 1.0, np.array([np.sin(a), .25, np.cos(a)]) * max(length, 4.5 * .85)
        else:
            e = 0.0
        if e > 0: limbs.append((root, foot, length if mode != "strike" else 4, rad, e, False))
    if mode == "walk":
        limbs.append((np.array([0, .8, .36]), np.array([1.1, .65, 5.0]), 9, .14, 1.0, True))
    return limbs

def draw(ax, mode, title):
    B = body_mesh()
    faces, colors = [], []
    light = norm([-.4, .8, -.5])
    def add_quad(q, base):
        n = np.cross(q[1] - q[0], q[2] - q[0])
        if np.linalg.norm(n) < 1e-9: return
        n = norm(n); s = .35 + .65 * max(0, n @ light)
        spec = max(0, n @ light) ** 18 * .5
        faces.append(q[:, [0, 2, 1]]); colors.append(np.clip(np.array(base) * s + spec, 0, 1))
    for i in range(B.shape[0] - 1):
        for j in range(B.shape[1] - 1):
            add_quad(np.array([B[i, j], B[i + 1, j], B[i + 1, j + 1], B[i, j + 1]]), (.17, .1, .24))
    for root, foot, length, rad, e, grasp in pose(mode):
        rings, point = limb(root, foot, length, rad, e, grasp)
        for k in range(len(rings) - 1):
            col = (.86, .8, .7) if k >= len(rings) - 4 else (.15, .09, .21)
            for s in range(len(rings[k]) - 1):
                add_quad(np.array([rings[k][s], rings[k + 1][s], rings[k + 1][s + 1], rings[k][s + 1]]), col)
        last = rings[-1]
        for s in range(len(last) - 1):
            tri = np.array([last[s], point, last[s + 1]])
            n = norm(np.cross(tri[1] - tri[0], tri[2] - tri[0]))
            faces.append(tri[:, [0, 2, 1]]); colors.append(np.clip(np.array((.86, .8, .7)) * (.4 + .6 * max(0, n @ light)), 0, 1))
    for d, size in ((norm([-.3, .9, .25]), .13), (norm([.25, .75, .55]), .085), (norm([-.55, .7, -.3]), .065), (norm([.5, .6, -.1]), .05), (norm([-.05, .8, -.6]), .04)):
        c = surface(d) * .96 + np.array([0, .36, 0])
        u, v = np.mgrid[0:np.pi:8j, 0:2 * np.pi:12j]
        ax.plot_surface(c[0] + size * np.sin(u) * np.cos(v), c[2] + size * np.sin(u) * np.sin(v), c[1] + size * np.cos(u),
                        color=(1, .2, .45), shade=False, zorder=10)
    coll = Poly3DCollection(faces, facecolors=colors, edgecolors="none")
    ax.add_collection3d(coll)
    floor = 4.3
    ax.plot_surface(*np.meshgrid([-floor, floor], [-floor, floor]), np.zeros((2, 2)) - .01, color=(.3, .36, .4), alpha=.35, shade=False)
    ax.set_xlim(-floor, floor); ax.set_ylim(-floor, floor); ax.set_zlim(-.3, 2.6)
    ax.set_box_aspect((1, 1, .4)); ax.view_init(elev=36, azim=-62); ax.set_axis_off()
    ax.set_title(title, color="w", fontsize=13)

fig = plt.figure(figsize=(15, 5.4), facecolor=(.1, .12, .15))
for k, (mode, title) in enumerate((("walk", "Хода: опори + хапальне щупальце"), ("strike", "Удар шипами: усі кінцівки назовні"),
                                   ("idle", "Спокій: кінцівки повністю втягнуті"))):
    ax = fig.add_subplot(1, 3, k + 1, projection="3d", facecolor=(.1, .12, .15))
    draw(ax, mode, title)
fig.text(.5, .02, "Попередній перегляд за формулами SlimeVisual (numpy/matplotlib). Не рендер Unity: матеріали й світло приблизні.",
         ha="center", color=(.75, .8, .8), fontsize=10)
plt.subplots_adjust(left=0, right=1, top=.92, bottom=.05, wspace=0)
plt.savefig(sys.argv[1] if len(sys.argv) > 1 else "slime_preview.png", dpi=110, facecolor=fig.get_facecolor())
