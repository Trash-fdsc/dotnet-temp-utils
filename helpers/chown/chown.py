#!/usr/bin/env python3
# ln -s /Arcs/Repos/smalls/dotnet-temp-utils/helpers/chown/chown.py /usr/local/bin/chown_inet

import os
import sys
import stat
import pwd
import grp
import shutil
from pathlib import Path

# --- НАСТРОЙКИ ---
# Список разрешённых пользователей (замените на своих)
ALLOWED_USERS = ["undelete", "deluge", "wget", "opera", "mega", ""]

# Целевой владелец и группа
TARGET_OWNER = "inet"
TARGET_GROUP = "arcs-read"

try:
    TARGET_OWNER_UID = pwd.getpwnam(TARGET_OWNER).pw_uid
    TARGET_OWNER_GID = grp.getgrnam(TARGET_GROUP).gr_gid
except KeyError as e:
    print(f"Ошибка: пользователь {TARGET_OWNER} или группа {TARGET_GROUP} не найдены: {e}", file=sys.stderr)
    sys.exit(1)


# Права для файлов (rw-r-----)
FILE_PERMS = 0o640

# Права для директорий (rwxr-x---)
DIR_PERMS = 0o750


def get_username_by_uid(uid: int) -> str:
    """Возвращает имя пользователя по UID."""
    try:
        return pwd.getpwuid(uid).pw_name
    except KeyError:
        # Если пользователя нет в passwd, возвращаем UID как строку
        return "" # str(uid)


def remove_immutable_attr(path: Path) -> None:
    """
    Снимает атрибут 'i' (immutable) с файла/директории через chattr.
    Требует прав root. Игнорирует ошибки, если атрибут не установлен.
    """
    # Используем os.system для вызова chattr, так как нет стандартного модуля Python для этого
    result = os.system(f"chattr -i '{path}' 2>/dev/null")
    # chattr может вернуть ненулевой код, если атрибут уже снят — это не ошибка

def isAllowedOwner(path: Path):

    if not path.exists():
        return False

    stat_info = path.stat()
    current_owner_name = get_username_by_uid(stat_info.st_uid)
    if current_owner_name not in ALLOWED_USERS:
        return False
    
    return True

def process_path(path: Path) -> None:
    
    if not isAllowedOwner(path):
        print(
            f"Ошибка: владелец '{current_owner_name}' файла '{path}' не входит в список разрешённых ({', '.join(ALLOWED_USERS)}).",
            file=sys.stderr
        )
        return
    
    """Применяет изменения к одному файлу или директории (не символьной ссылке)."""
    # 1. Снимаем immutable атрибут
    remove_immutable_attr(path)

    # 2. Меняем владельца и группу
    os.chown(path, TARGET_OWNER_UID, TARGET_OWNER_GID)

    # 3. Устанавливаем права доступа
    if path.is_dir():
        os.chmod(path, DIR_PERMS)
    else:
        os.chmod(path, FILE_PERMS)


def main():
    if len(sys.argv) != 2:
        print("Ошибка: требуется ровно один аргумент — путь к файлу или директории.", file=sys.stderr)
        sys.exit(1)

    if os.geteuid() != 0:
        print("Ошибка: скрипт должен быть запущен от root (требуется для chattr и chown).", file=sys.stderr)
        sys.exit(1)

    if shutil.which("chattr") is None:
        print("Ошибка: утилита chattr не найдена в PATH.", file=sys.stderr)
        sys.exit(1)

    target_path_str = sys.argv[1]
    target_path = Path(target_path_str).resolve()

    if not target_path.exists():
        print(f"Ошибка: путь '{target_path}' не существует.", file=sys.stderr)
        sys.exit(1)

    # Если сам целевой путь — символическая ссылка, игнорируем его полностью
    if target_path.is_symlink():
        print(f"Игнорируется: целевой путь является символической ссылкой: {target_path}", file=sys.stderr)
        sys.exit(0)

    # Проверка: входит ли владелец в список разрешённых
    if not isAllowedOwner(target_path):
        print(
            f"Ошибка: владелец файла '{target_path}' не входит в список разрешённых ({', '.join(ALLOWED_USERS)}).",
            file=sys.stderr
        )
        sys.exit(1)

    # Обработка: если директория — рекурсивно, если файл — только он
    if target_path.is_dir():
        # Проходим по всем элементам, включая саму директорию
        for root, dirs, files in os.walk(target_path, topdown=True):
            current_root = Path(root)

            # Если сама директория — симлинк (маловероятно при таком обходе, но на всякий случай), пропускаем
            if current_root.is_symlink():
                continue

            # Сначала обрабатываем директорию
            process_path(current_root)

            # Затем файлы внутри
            for filename in files:
                file_path = current_root / filename
                # Игнорируем файлы-ссылки
                if file_path.is_symlink():
                    continue
                process_path(file_path)

            # Исключаем из обхода поддиректории, которые являются симлинками
            # Это предотвращает вход внутрь ссылок на директории
            # dirs[:] = [d for d in dirs if not (current_root / d).is_symlink()]
            dirs[:] = [
                d for d in dirs if not (current_root / d).is_symlink()
            ]

    else:
        # Если это обычный файл (не ссылка) — обрабатываем только его
        process_path(target_path)

    print(f"Успешно обработан путь: {target_path}")


if __name__ == "__main__":
    main()

