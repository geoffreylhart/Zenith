bl_info = {
    "name": "Z-Addon",
    "author": "Geoffrey Hart",
    "blender": (2, 79, 0),
    "location": "View3D",
    "description": "Loads arbitrary driver functions",
    "category": "Development",
}

"""
This script contains a glut of custom driver functions to call and use
"""

import bpy, bmesh

# calculates the length of curve splines
def calc_length(x):
	return sum(s.calc_length() for s in x)

# calculates the length of a curve by name after modifiers have been applied
def calc_mesh_length(name):
    curve = bpy.data.objects[name]
    mesh = curve.to_mesh(bpy.context.scene, True, "PREVIEW")
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.edges.ensure_lookup_table()
    meshLength = sum(e.calc_length() for e in bm.edges)
    return meshLength

@bpy.app.handlers.persistent
def load_handler(dummy):
    bpy.app.driver_namespace["calc_length"] = calc_length
    bpy.app.driver_namespace["calc_mesh_length"] = calc_mesh_length

def register():
    load_handler(None)
    bpy.app.handlers.load_post.append(load_handler)

def unregister():
    bpy.app.handlers.load_post.remove(load_handler)

if __name__ == "__main__":
    register()