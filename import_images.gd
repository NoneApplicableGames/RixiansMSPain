extends SceneTree

var image_input_dir: String = ""
var tres_output_dir: String = ""

func _init() -> void:
	var user_args = OS.get_cmdline_user_args()
	
	# Dynamically retrieve this script's own path on disk
	var script_path: String = get_script().resource_path
	
	# Require at least 2 positional arguments after '--'
	if user_args.size() < 2:
		print("❌ Error: Missing required path arguments!")
		print("\n📖 Usage:")
		print("  godot --headless -s %s -- <input_dir> <output_dir>" % script_path)
		print("\n💡 Example:")
		print("  godot --headless -s %s -- res://HideDetailsMod/artist_assets/ res://images/atlases/card_atlas.sprites/\n" % script_path)
		quit(1)
		return
		
	image_input_dir = user_args[0]
	tres_output_dir = user_args[1]
	
	print("🚀 Starting asset sync...")
	print("  📂 Input Dir:  ", image_input_dir)
	print("  📂 Output Dir: ", tres_output_dir)

	if not DirAccess.dir_exists_absolute(image_input_dir):
		DirAccess.make_dir_recursive_absolute(image_input_dir)
		print("ℹ️ Input directory created: ", image_input_dir)
		quit(0)
		return

	if not DirAccess.dir_exists_absolute(tres_output_dir):
		DirAccess.make_dir_recursive_absolute(tres_output_dir)

	var expected_tres_paths: Dictionary = {}
	var processed_stats = _process_directory_recursive(image_input_dir, expected_tres_paths)
	print("📊 Summary: %d updated, %d skipped." % [processed_stats["updated"], processed_stats["skipped"]])

	var removed_count = _prune_orphaned_tres_files(tres_output_dir, expected_tres_paths)
	if removed_count > 0:
		print("🧹 Cleaned up %d orphaned file(s)." % removed_count)

	print("🎉 Done!")
	quit(0)


# Recursively processes source images and tracks expected target .tres paths
func _process_directory_recursive(current_dir_path: String, expected_tres_paths: Dictionary) -> Dictionary:
	var stats = {"updated": 0, "skipped": 0}
	var dir = DirAccess.open(current_dir_path)
	if dir == null:
		print("❌ Could not open folder: ", current_dir_path)
		return stats
		
	dir.list_dir_begin()
	var item_name = dir.get_next()
	
	while item_name != "":
		if item_name != "." and item_name != "..":
			var item_full_path = current_dir_path.path_join(item_name)
			
			if dir.current_is_dir():
				var sub_stats = _process_directory_recursive(item_full_path, expected_tres_paths)
				stats["updated"] += sub_stats["updated"]
				stats["skipped"] += sub_stats["skipped"]
			else:
				var ext = item_name.get_extension().to_lower()
				if ext in ["png", "jpg", "jpeg", "webp"]:
					var relative_sub_path = current_dir_path.replace(image_input_dir, "")
					var destination_folder = tres_output_dir.path_join(relative_sub_path)
					var destination_tres_path = destination_folder.path_join(item_name.get_basename() + ".tres")
					
					expected_tres_paths[destination_tres_path] = true
					
					if _should_update_resource(item_full_path, destination_tres_path):
						if not DirAccess.dir_exists_absolute(destination_folder):
							DirAccess.make_dir_recursive_absolute(destination_folder)
						
						var texture_resource = load(item_full_path)
						if texture_resource is Texture2D:
							var error = ResourceSaver.save(texture_resource, destination_tres_path)
							if error == OK:
								print("✅ Updated resource: ", destination_tres_path)
								stats["updated"] += 1
							else:
								print("❌ Serialization failed for ", item_name, " - Code: ", error)
						else:
							print("⚠️ File skipped: Cannot parse ", item_full_path, " into Texture2D. (", get_type_string(texture_resource), ")")
					else:
						stats["skipped"] += 1
						
		item_name = dir.get_next()
		
	dir.list_dir_end()
	return stats


# Checks if target .tres is missing or outdated compared to source image timestamp
func _should_update_resource(source_image_path: String, target_tres_path: String) -> bool:
	if not FileAccess.file_exists(target_tres_path):
		return true
		
	var source_mtime = FileAccess.get_modified_time(source_image_path)
	var target_mtime = FileAccess.get_modified_time(target_tres_path)
	
	return source_mtime > target_mtime


# Removes .tres files in destination that are no longer present in expected set
func _prune_orphaned_tres_files(current_dir_path: String, expected_tres_paths: Dictionary) -> int:
	var removed_count = 0
	if not DirAccess.dir_exists_absolute(current_dir_path):
		return 0
		
	var dir = DirAccess.open(current_dir_path)
	if dir == null:
		return 0
		
	dir.list_dir_begin()
	var item_name = dir.get_next()
	var remaining_items = 0
	
	while item_name != "":
		if item_name != "." and item_name != "..":
			var item_full_path = current_dir_path.path_join(item_name)
			
			if dir.current_is_dir():
				removed_count += _prune_orphaned_tres_files(item_full_path, expected_tres_paths)
				if _is_directory_empty(item_full_path):
					DirAccess.remove_absolute(item_full_path)
				else:
					remaining_items += 1
			else:
				if item_name.get_extension().to_lower() == "tres":
					if not expected_tres_paths.has(item_full_path):
						var err = DirAccess.remove_absolute(item_full_path)
						if err == OK:
							print("🗑️ Removed orphaned resource: ", item_full_path)
							removed_count += 1
						else:
							print("❌ Failed to remove orphaned file ", item_full_path, " - Code: ", err)
					else:
						remaining_items += 1
				else:
					remaining_items += 1
					
		item_name = dir.get_next()
		
	dir.list_dir_end()
	return removed_count


# Helper to check if a directory contains any files or folders
func _is_directory_empty(dir_path: String) -> bool:
	var dir = DirAccess.open(dir_path)
	if dir == null:
		return true
		
	dir.list_dir_begin()
	var item = dir.get_next()
	while item != "":
		if item != "." and item != "..":
			dir.list_dir_end()
			return false
		item = dir.get_next()
		
	dir.list_dir_end()
	return true

# Stuff

## Returns the string type name of any variable, including engine classes, 
## custom `class_name` scripts, built-in types, and null.
func get_type_string(any_var) -> String:
	# 1. Handle Null / Nil explicitly first
	if any_var == null:
		return "null"

	var type_id = typeof(any_var)
	
	# 2. Handle Objects (Engine nodes, resources, custom classes)
	if type_id == TYPE_OBJECT:
		# Check if the object is freed/invalidated (safety check)
		if not is_instance_valid(any_var):
			return "freed_object"
			
		var script = any_var.get_script()
		if script is Script:
			var global_name = script.get_global_name()
			if global_name != &"":
				return String(global_name) # Returns custom `class_name`
			
			# Fallback for anonymous scripts attached to a node (returns script path)
			if script.resource_path != "":
				return script.resource_path.get_file()
				
		return any_var.get_class() # Returns engine type (e.g., "Sprite2D", "RefCounted")

	# 3. Handle Built-in Types (int, float, Vector2, Array, Dictionary, etc.)
	return _get_builtin_type_name(type_id)


## Helper function mapping Variant type integers to human-readable strings
func _get_builtin_type_name(type_id: int) -> String:
	match type_id:
		TYPE_BOOL: return "bool"
		TYPE_INT: return "int"
		TYPE_FLOAT: return "float"
		TYPE_STRING: return "String"
		TYPE_VECTOR2: return "Vector2"
		TYPE_VECTOR2I: return "Vector2i"
		TYPE_RECT2: return "Rect2"
		TYPE_RECT2I: return "Rect2i"
		TYPE_VECTOR3: return "Vector3"
		TYPE_VECTOR3I: return "Vector3i"
		TYPE_TRANSFORM2D: return "Transform2D"
		TYPE_VECTOR4: return "Vector4"
		TYPE_VECTOR4I: return "Vector4i"
		TYPE_PLANE: return "Plane"
		TYPE_QUATERNION: return "Quaternion"
		TYPE_AABB: return "AABB"
		TYPE_BASIS: return "Basis"
		TYPE_TRANSFORM3D: return "Transform3D"
		TYPE_PROJECTION: return "Projection"
		TYPE_COLOR: return "Color"
		TYPE_STRING_NAME: return "StringName"
		TYPE_NODE_PATH: return "NodePath"
		TYPE_RID: return "RID"
		TYPE_CALLABLE: return "Callable"
		TYPE_SIGNAL: return "Signal"
		TYPE_DICTIONARY: return "Dictionary"
		TYPE_ARRAY: return "Array"
		TYPE_PACKED_BYTE_ARRAY: return "PackedByteArray"
		TYPE_PACKED_INT32_ARRAY: return "PackedInt32Array"
		TYPE_PACKED_INT64_ARRAY: return "PackedInt64Array"
		TYPE_PACKED_FLOAT32_ARRAY: return "PackedFloat32Array"
		TYPE_PACKED_FLOAT64_ARRAY: return "PackedFloat64Array"
		TYPE_PACKED_STRING_ARRAY: return "PackedStringArray"
		TYPE_PACKED_VECTOR2_ARRAY: return "PackedVector2Array"
		TYPE_PACKED_VECTOR3_ARRAY: return "PackedVector3Array"
		TYPE_PACKED_COLOR_ARRAY: return "PackedColorArray"
		TYPE_PACKED_VECTOR4_ARRAY: return "PackedVector4Array"
		_: return "Unknown"
