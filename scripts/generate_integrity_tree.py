import os, sys
import json
import hashlib
try:
    
    if len(sys.argv) < 2:
        raise ValueError("A directory path must be provided as an argument.")

    root_dir = sys.argv[1]
    if not os.path.isdir(root_dir):
        raise FileNotFoundError(f"The directory '{root_dir}' does not exist.")

    integrity_data = {}
    script_name = os.path.basename(__file__)
    output_filename = "IntegrityTree.json"

    for subdir, _, files in os.walk(root_dir):
        for filename in files:
            if filename == script_name or filename == output_filename:
                continue

            file_path = os.path.join(subdir, filename)
            relative_path = os.path.relpath(file_path, root_dir).replace('\\', '/')
            print(f" - Computing MD5SUM of {relative_path}...")

            with open(file_path, 'rb') as f:
                md5_hash = hashlib.md5(f.read())
            
            integrity_data[relative_path] = md5_hash.hexdigest()

    output_file_path = os.path.join(root_dir, output_filename)
    with open(output_file_path, 'w') as f:
        json.dump(integrity_data, f, indent=4, sort_keys=True)

    print(f"Integrity tree was generated and saved to {root_dir}/{output_filename}")
    
    
    
    
except Exception as e:
    print(e)
    os.system("pause")