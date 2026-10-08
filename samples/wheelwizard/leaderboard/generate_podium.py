"""Regenerate the bundled podium GLB and its headless geometry preview. No editor or remote assets required."""

from __future__ import annotations

import json
import math
import pathlib
import struct

HERE = pathlib.Path(__file__).resolve().parent
PLACEMENTS = [(0.0, 70.0, 0.0), (-132.0, 46.0, 0.0), (132.0, 28.0, 0.0)]
MATERIALS = [
    ("gold", [0.83, 0.52, 0.10, 1.0], 0.72, 0.34),
    ("silver", [0.65, 0.72, 0.81, 1.0], 0.62, 0.38),
    ("bronze", [0.53, 0.26, 0.11, 1.0], 0.62, 0.42),
    ("dark_base", [0.045, 0.058, 0.085, 1.0], 0.20, 0.62),
    ("number_ivory", [0.96, 0.93, 0.83, 1.0], 0.15, 0.45),
]


def sub(a, b):
    return tuple(x - y for x, y in zip(a, b))


def dot(a, b):
    return sum(x * y for x, y in zip(a, b))


def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def normalized(a):
    length = math.sqrt(dot(a, a))
    assert length > 1e-8
    return tuple(v / length for v in a)


class Mesh:
    def __init__(self, name, material):
        self.name = name
        self.material = material
        self.positions = []
        self.normals = []
        self.indices = []

    def polygon(self, vertices, outside):
        vertices = list(vertices)
        normal = cross(sub(vertices[1], vertices[0]), sub(vertices[2], vertices[0]))
        if dot(normal, outside) < 0:
            vertices.reverse()
            normal = cross(sub(vertices[1], vertices[0]), sub(vertices[2], vertices[0]))
        normal = normalized(normal)
        base = len(self.positions)
        self.positions.extend(vertices)
        self.normals.extend([normal] * len(vertices))
        for i in range(1, len(vertices) - 1):
            self.indices.extend([base, base + i, base + i + 1])

    def box(self, center, size):
        x, y, z = center
        w, h, d = (v / 2 for v in size)
        for normal, vertices in [
            ((1, 0, 0), [(x + w, y - h, z - d), (x + w, y + h, z - d), (x + w, y + h, z + d), (x + w, y - h, z + d)]),
            ((-1, 0, 0), [(x - w, y - h, z - d), (x - w, y - h, z + d), (x - w, y + h, z + d), (x - w, y + h, z - d)]),
            ((0, 1, 0), [(x - w, y + h, z - d), (x - w, y + h, z + d), (x + w, y + h, z + d), (x + w, y + h, z - d)]),
            ((0, -1, 0), [(x - w, y - h, z - d), (x + w, y - h, z - d), (x + w, y - h, z + d), (x - w, y - h, z + d)]),
            ((0, 0, 1), [(x - w, y - h, z + d), (x + w, y - h, z + d), (x + w, y + h, z + d), (x - w, y + h, z + d)]),
            ((0, 0, -1), [(x - w, y - h, z - d), (x - w, y + h, z - d), (x + w, y + h, z - d), (x + w, y - h, z - d)]),
        ]:
            self.polygon(vertices, normal)

    def beveled_plinth(self, x, bottom, top, width, depth, bevel):
        # Eight-sided rings keep both the outline and top edge softened while the standing surface stays planar.
        def ring(y, inset):
            w, d, c = width / 2 - inset, depth / 2 - inset, 5.0
            return [(x - w + c, y, -d), (x + w - c, y, -d), (x + w, y, -d + c), (x + w, y, d - c),
                    (x + w - c, y, d), (x - w + c, y, d), (x - w, y, d - c), (x - w, y, -d + c)]
        rings = [ring(bottom, bevel), ring(bottom + bevel, 0), ring(top - bevel, 0), ring(top, bevel)]
        self.polygon(rings[0], (0, -1, 0))
        self.polygon(rings[-1], (0, 1, 0))
        for lower, upper in zip(rings, rings[1:]):
            for i in range(8):
                j = (i + 1) % 8
                outside = ((lower[i][0] + lower[j][0]) / 2 - x, 0, (lower[i][2] + lower[j][2]) / 2)
                self.polygon([lower[i], lower[j], upper[j], upper[i]], outside)


def make_meshes():
    meshes = []
    base = Mesh("podium_base", 3)
    base.beveled_plinth(0, 0, 10, 430, 116, 2)
    meshes.append(base)
    for index, (x, top, _) in enumerate(PLACEMENTS):
        mesh = Mesh(["first_gold", "second_silver", "third_bronze"][index], index)
        mesh.beveled_plinth(x, 10, top, 132, 96, 2)
        meshes.append(mesh)
        label = Mesh(f"placement_{index + 1}", 4)
        y = (top + 10) / 2
        height = [23, 18, 12][index]
        thickness = height * 0.13
        if index == 0:
            label.box((x, y, 49), (thickness, height, 2))
            label.box((x - thickness * 0.4, y + height / 2 - thickness * 0.8, 49), (thickness * 2, thickness, 2))
            label.box((x, y - height / 2 + thickness / 2, 49), (thickness * 3.2, thickness, 2))
        else:
            width = height * 0.6
            # Raised, extruded seven-segment numerals: no font texture or external image dependency.
            parts = {
                "top": ((x, y + height / 2 - thickness / 2, 49), (width, thickness, 2)),
                "mid": ((x, y, 49), (width, thickness, 2)),
                "bottom": ((x, y - height / 2 + thickness / 2, 49), (width, thickness, 2)),
                "upper_right": ((x + width / 2 - thickness / 2, y + height / 4, 49), (thickness, height / 2 - thickness, 2)),
                "lower_right": ((x + width / 2 - thickness / 2, y - height / 4, 49), (thickness, height / 2 - thickness, 2)),
                "lower_left": ((x - width / 2 + thickness / 2, y - height / 4, 49), (thickness, height / 2 - thickness, 2)),
            }
            for segment in ["top", "mid", "bottom", "upper_right", "lower_left" if index == 1 else "lower_right"]:
                label.box(*parts[segment])
        meshes.append(label)
    return meshes


def write_glb(meshes):
    binary = bytearray()
    views, accessors, gltf_meshes, nodes = [], [], [], []

    def accessor(values, components, target, integer=False):
        while len(binary) % 4:
            binary.append(0)
        offset = len(binary)
        flat = values if components == 1 else [v for row in values for v in row]
        binary.extend(struct.pack("<" + ("H" if integer else "f") * len(flat), *flat))
        views.append({"buffer": 0, "byteOffset": offset, "byteLength": len(binary) - offset, "target": target})
        a = {"bufferView": len(views) - 1, "componentType": 5123 if integer else 5126,
             "count": len(values), "type": "SCALAR" if components == 1 else f"VEC{components}"}
        if target == 34962 and components == 3:
            a["min"] = [min(row[i] for row in values) for i in range(3)]
            a["max"] = [max(row[i] for row in values) for i in range(3)]
        accessors.append(a)
        return len(accessors) - 1

    for mesh in meshes:
        assert len(mesh.positions) == len(mesh.normals)
        assert len(mesh.indices) % 3 == 0
        for offset in range(0, len(mesh.indices), 3):
            a, b, c = [mesh.positions[i] for i in mesh.indices[offset:offset + 3]]
            winding = cross(sub(b, a), sub(c, a))
            assert dot(winding, mesh.normals[mesh.indices[offset]]) > 1e-6, mesh.name
        assert all(abs(dot(n, n) - 1) < 1e-6 for n in mesh.normals)
        primitive = {"attributes": {"POSITION": accessor(mesh.positions, 3, 34962), "NORMAL": accessor(mesh.normals, 3, 34962)},
                     "indices": accessor(mesh.indices, 1, 34963, integer=True), "material": mesh.material, "mode": 4}
        gltf_meshes.append({"name": mesh.name, "primitives": [primitive]})
        nodes.append({"name": mesh.name, "mesh": len(gltf_meshes) - 1})
    for role, position in zip(["first", "second", "third"], PLACEMENTS):
        nodes.append({"name": role + "_anchor", "translation": list(position), "extras": {"role": role, "podiumTop": position[1]}})
    default_scale = [0.8325, 0.885, 0.8325]
    gltf = {"asset": {"version": "2.0", "generator": "podium mesh generator"}, "scene": 0,
            "scenes": [{"name": "leaderboard_podium", "nodes": list(range(len(nodes)))}], "nodes": nodes,
            "meshes": gltf_meshes, "accessors": accessors, "bufferViews": views, "buffers": [{"byteLength": len(binary)}],
            "materials": [{"name": name, "pbrMetallicRoughness": {"baseColorFactor": color, "metallicFactor": metal, "roughnessFactor": rough}}
                          for name, color, metal, rough in MATERIALS],
            "extras": {"units": "canonical", "up": "+Y", "front": "+Z", "canonicalToRender": 0.7,
                       "defaultBodyScale": default_scale, "podiumToStageScale": [v * 0.7 for v in default_scale]}}
    json_chunk = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
    json_chunk += b" " * (-len(json_chunk) % 4)
    binary += b"\0" * (-len(binary) % 4)
    total = 12 + 8 + len(json_chunk) + 8 + len(binary)
    data = struct.pack("<III", 0x46546C67, 2, total)
    data += struct.pack("<II", len(json_chunk), 0x4E4F534A) + json_chunk
    data += struct.pack("<II", len(binary), 0x004E4942) + binary
    path = HERE / "podium.glb"
    path.write_bytes(data)
    return path, gltf


def preview(meshes):
    from array import array
    from PIL import Image, ImageDraw, ImageFont

    image = Image.new("RGB", (1280, 640), (20, 26, 39))
    draw = ImageDraw.Draw(image)
    right = normalized((0.97, 0, -0.24))
    forward = normalized((-0.24, -0.43, -0.87))
    up = normalized(cross(right, forward))
    light = normalized((-0.35, 0.8, 0.55))

    def project(point):
        return (640 + dot(point, right) * 2.45, 435 - dot(point, up) * 2.45)

    pixels = image.load()
    depths = array("f", [float("inf")]) * (1280 * 640)
    for mesh in meshes:
        color = MATERIALS[mesh.material][1][:3]
        for i in range(0, len(mesh.indices), 3):
            points = [mesh.positions[j] for j in mesh.indices[i:i + 3]]
            normal = mesh.normals[mesh.indices[i]]
            if dot(normal, forward) >= 0:
                continue
            shade = 0.52 + 0.48 * max(0, dot(normal, light))
            rgb = tuple(int(255 * math.sqrt(c) * shade) for c in color)
            screen = [project(p) for p in points]
            triangle_depth = [dot(p, forward) for p in points]
            (ax, ay), (bx, by), (cx, cy) = screen
            determinant = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
            if abs(determinant) < 1e-8:
                continue
            for py in range(max(0, int(min(ay, by, cy))), min(640, math.ceil(max(ay, by, cy)) + 1)):
                for px in range(max(0, int(min(ax, bx, cx))), min(1280, math.ceil(max(ax, bx, cx)) + 1)):
                    u = ((by - cy) * (px + 0.5 - cx) + (cx - bx) * (py + 0.5 - cy)) / determinant
                    v = ((cy - ay) * (px + 0.5 - cx) + (ax - cx) * (py + 0.5 - cy)) / determinant
                    w = 1 - u - v
                    if min(u, v, w) < -1e-6:
                        continue
                    depth = u * triangle_depth[0] + v * triangle_depth[1] + w * triangle_depth[2]
                    offset = py * 1280 + px
                    if depth < depths[offset]:
                        depths[offset] = depth
                        pixels[px, py] = rgb
    try:
        large = ImageFont.truetype("C:/Windows/Fonts/segoeuib.ttf", 34)
        small = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 21)
    except OSError:
        large = small = ImageFont.load_default()
    draw.text((54, 34), "Leaderboard podium", fill=(235, 239, 246), font=large)
    draw.text((56, 81), "Bundled GLB  /  gold, silver, bronze  /  matching standing anchors", fill=(161, 178, 203), font=small)
    for label, position in zip(["FIRST  y=70", "SECOND  y=46", "THIRD  y=28"], PLACEMENTS):
        x, y = project(position)
        draw.ellipse((x - 12, y - 4, x + 12, y + 4), outline=(247, 247, 255), width=2)
        draw.line((x, y - 7, x, y - 65), fill=(165, 187, 213), width=2)
        width = draw.textlength(label, font=small)
        draw.text((x - width / 2, y - 96), label, fill=(218, 227, 240), font=small)
    draw.text((56, 583), "Y up  /  front +Z  /  geometry units are canonical; see README for stage scaling", fill=(153, 172, 199), font=small)
    path = HERE / "podium_preview.png"
    image.save(path)
    return path


if __name__ == "__main__":
    meshes = make_meshes()
    path, gltf = write_glb(meshes)
    preview_path = preview(meshes)
    print(f"Wrote {path.name}: {path.stat().st_size} bytes, {len(gltf['meshes'])} meshes, {sum(len(m.indices) // 3 for m in meshes)} triangles")
    print(f"Wrote {preview_path.name}; triangle winding and unit normals checked")
