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

import bpy

# calculates the length of curve splines
def calc_length(x):
	return sum(s.calc_length() for s in x)

@bpy.app.handlers.persistent
def load_handler(dummy):
    bpy.app.driver_namespace["calc_length"] = calc_length

def register():
    load_handler(None)
    bpy.app.handlers.load_post.append(load_handler)

def unregister():
    bpy.app.handlers.load_post.remove(load_handler)

if __name__ == "__main__":
    register()