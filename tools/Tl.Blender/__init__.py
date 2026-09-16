bl_info = {
    "name": "tl bridge",
    "author": "tl",
    "blender": (5, 0, 0),
    "version": (1, 0, 0),
    "location": "View3D > Sidebar > tl",
    "description": "Export Blender NLA scenes to tl flat-schema-v1 authoring JSON and bake .tlb assets",
    "category": "Animation",
}

import os

import bpy

from . import mapping


def _addon_preferences(context):
    return context.preferences.addons[__package__].preferences


def _mapping_prefs(context):
    preferences = _addon_preferences(context)
    return mapping.Prefs(
        default_track_type=preferences.default_track_type.strip(),
        default_clip_type=preferences.default_clip_type.strip(),
        namespace=preferences.namespace.strip(),
    )


class TL_OT_export_timelines(bpy.types.Operator):
    bl_idname = "tl.export_timelines"
    bl_label = "Export tl timelines"
    bl_description = (
        "Map the selected objects' NLA tracks to tl timelines and write flat-schema-v1 authoring JSON"
    )

    directory: bpy.props.StringProperty(subtype="DIR_PATH", name="Output directory")

    @classmethod
    def poll(cls, context):
        return bool(context.selected_objects)

    def invoke(self, context, event):
        context.window_manager.fileselect_add(self)
        return {"RUNNING_MODAL"}

    def execute(self, context):
        if not self.directory:
            self.report({"ERROR"}, "choose an output directory for the tl authoring JSON")
            return {"CANCELLED"}
        try:
            exported = mapping.export_objects(
                context.scene,
                list(context.selected_objects),
                _mapping_prefs(context),
                bpy.app.version,
            )
        except mapping.MappingError as error:
            self.report({"ERROR"}, str(error))
            return {"CANCELLED"}
        for severity, message in exported.report:
            self.report(_blender_severity(severity), message)
        directory = bpy.path.abspath(self.directory)
        for filename in sorted(exported.files):
            with open(os.path.join(directory, filename), "w", encoding="utf-8", newline="\n") as handle:
                handle.write(exported.files[filename])
        self.report({"INFO"}, "wrote %d timeline JSON file(s) to %s" % (len(exported.files), directory))
        return {"FINISHED"}


def _blender_severity(severity):
    return {"WARNING"} if severity == mapping.SEVERITY_WARNING else {"INFO"}


class TL_PT_sidebar(bpy.types.Panel):
    bl_label = "tl timelines"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "tl"

    def draw(self, context):
        column = self.layout.column()
        column.enabled = TL_OT_export_timelines.poll(context)
        column.operator(TL_OT_export_timelines.bl_idname)
        self.layout.operator("preferences.addon_show", text="Preferences").module = __package__


class TL_AddonPreferences(bpy.types.AddonPreferences):
    bl_idname = __package__

    default_track_type: bpy.props.StringProperty(
        name="Default track type",
        description="Bare track type name used when a track has no tl_track entry; empty rejects those tracks",
        default="",
    )
    default_clip_type: bpy.props.StringProperty(
        name="Default clip type",
        description="Bare clip type name used when a strip has no tl_clip entry; empty rejects those strips",
        default="",
    )
    namespace: bpy.props.StringProperty(
        name="Namespace",
        description="Bare namespace written on every track and clip; empty selects the global namespace",
        default="",
    )

    def draw(self, context):
        layout = self.layout
        layout.prop(self, "default_track_type")
        layout.prop(self, "default_clip_type")
        layout.prop(self, "namespace")


_classes = (
    TL_AddonPreferences,
    TL_OT_export_timelines,
    TL_PT_sidebar,
)


def register():
    for cls in _classes:
        bpy.utils.register_class(cls)


def unregister():
    for cls in reversed(_classes):
        bpy.utils.unregister_class(cls)
