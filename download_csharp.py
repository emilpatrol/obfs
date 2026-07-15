from datasets import load_dataset
import os

HF_TOKEN = "" 

print("Подключение к StarCoderData (сабсет c-sharp)...")

ds = load_dataset(
    "bigcode/starcoderdata", 
    data_dir="c-sharp",
    split="train", 
    streaming=True, 
    token=HF_TOKEN
)

output_dir = "/home/gustav/storage/csharp_raw_sources"
os.makedirs(output_dir, exist_ok=True)

limit = 5000
count = 0

for item in ds:
    code = item.get('content')
    if not code:
        continue
        
    repo_name = item.get('repo_name', 'unknown_repo').replace("/", "_").replace("\\", "_")
    path = item.get('path', f"file_{count}.cs")
    safe_file_name = path.split('/')[-1]
    
    filename = f"{output_dir}/{repo_name}_{safe_file_name}"
    
    try:
        with open(filename, "w", encoding="utf-8") as f:
            f.write(code)
    except OSError:
        continue
        
    count += 1
    if count % 100 == 0:
        print(f"Скачано файлов: {count}/{limit}")
        
    if count >= limit:
        break

print("\nУспешно завершено! Исходники лежат в папке:", output_dir)
