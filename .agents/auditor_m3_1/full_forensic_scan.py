import os
import re

project_dir = r"c:\Dev\RummyBookyMaui\RummyBooky"

print("=================== FULL FORENSIC SCAN ===================")

violations = []

for root, dirs, files in os.walk(project_dir):
    if 'bin' in dirs:
        dirs.remove('bin')
    if 'obj' in dirs:
        dirs.remove('obj')
    for file in files:
        file_path = os.path.join(root, file)
        ext = os.path.splitext(file)[1].lower()
        if ext not in ('.xaml', '.cs'):
            continue
        
        with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
            lines = content.splitlines()

        # Check 1: Pure Black/White or Untinted Grays
        pure_bw_gray_pattern = re.compile(r'(#000000|#FFFFFF|#808080|#CCCCCC|Colors\.White|Colors\.Black|Color\.White|Color\.Black)')
        for idx, line in enumerate(lines):
            match = pure_bw_gray_pattern.search(line)
            if match:
                violation_str = f"Pure B/W or Untinted Gray in {os.path.relpath(file_path, project_dir)} (line {idx+1}): {line.strip()}"
                violations.append(violation_str)
                print(f"[FAIL] {violation_str}")

        # Check 2: Legacy Frame in XAML
        if ext == '.xaml':
            if '<Frame' in content:
                violation_str = f"Legacy <Frame> control in {os.path.relpath(file_path, project_dir)}"
                violations.append(violation_str)
                print(f"[FAIL] {violation_str}")

        # Check 3: StaticResource color bindings in non-dictionary XAML files
        if ext == '.xaml' and file not in ('Colors.xaml', 'Typography.xaml', 'Dimensions.xaml', 'Theme.xaml', 'Styles.xaml'):
            static_color_pattern = re.compile(r'(Color|BackgroundColor|TextColor|Stroke|Fill|BorderColor|PlaceholderColor|TitleColor|ShadowColor)\s*=\s*"\{StaticResource')
            for idx, line in enumerate(lines):
                if static_color_pattern.search(line):
                    violation_str = f"StaticResource color binding in {os.path.relpath(file_path, project_dir)} (line {idx+1}): {line.strip()}"
                    violations.append(violation_str)
                    print(f"[FAIL] {violation_str}")

print("\n=================== SCAN SUMMARY ===================")
print(f"Total violations found: {len(violations)}")
if len(violations) == 0:
    print("VERDICT: CLEAN")
else:
    print("VERDICT: INTEGRITY VIOLATION")
