#!/usr/bin/env python3
"""
Скрипт для проверки и установки ACL‑запретов на выполнение программ для указанных пользователей.
"""

import subprocess
import sys
import os
import argparse
from pathlib import Path

def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument(
        'printWell',
        nargs='?',  # необязательный позиционный аргумент
        default=False,  # значение по умолчанию
        const=True,   # если аргумент указан без значения (например, `python script.py`)
        type=lambda x: x.lower() in ('true', '1', 'yes', 'on', 'up'),
        help='Показывать проверенные, но неизменённые права (по умолчанию: False)'
    )
    
    return parser.parse_args()

# Использование
args = parse_args()
printWell = args.printWell
# print(f"Булев аргумент: {my_bool}")


def read_programs_file(file_path: str) -> list[str]:
    """Читает файл со списком программ (по одной на строку)."""
    try:
        programs = []
        with open(file_path, 'r', encoding='utf-8') as f:
            # programs = [line.strip() for line in f if line.strip()]
            for line in f:
                ln = line.strip()
                if ln and not ln.startswith("#"):
                    programs.append(ln)
            programs 
        return programs
    except FileNotFoundError:
        print(f"Ошибка: файл '{file_path}' не найден.")
        sys.exit(1)
    except PermissionError:
        print(f"Ошибка: нет прав на чтение файла '{file_path}'.")
        sys.exit(1)
    except Exception as e:
        print(f"Неожиданная ошибка при чтении файла: {e}")
        sys.exit(1)



def check_acl_restriction(program: str, user: str) -> bool:
    """
    Проверяет, что пользователю запрещены чтение, запись и исполнение для программы.
    Возвращает True, если запрет установлен, иначе False.
    """
    try:
        result = subprocess.run(['getfacl', program], capture_output=True, text=True, check=True)
        acl_output = result.stdout

        # Ищем строку с правами для пользователя
        for line in acl_output.splitlines():
            if line.startswith(f'user:{user}:'):
                # Формат: user:username:rwx или user:username:---
                permissions = line.split(':')[-1]
                return permissions == '---'

        # Если строки для пользователя нет, значит, права не установлены
        return False

    except subprocess.CalledProcessError as e:
        print(f"Ошибка getfacl для {program}: {e.stderr}")
        return False
    except Exception as e:
        print(f"Неожиданная ошибка при проверке ACL для {program}: {e}")
        return False




def set_acl_restriction(program: str, user: str) -> bool:
    """Устанавливает запрет на чтение, запись и исполнение для пользователя."""
    try:
        subprocess.run(
            ['setfacl', '-m', f'u:{user}:---', program],
            capture_output=True,
            text=True,
            check=True
        )
        if check_acl_restriction(program, user):
            print(f"Успешно установлен запрет для пользователя '{user}' на {program}")
        else:
            print(f"ОШИБКА при установлении запрет для пользователя '{user}' на {program}")
            
        return True
    except subprocess.CalledProcessError as e:
        print(f"Ошибка setfacl для {program} (пользователь {user}): {e.stderr}")
        return False
    except Exception as e:
        print(f"Неожиданная ошибка при установке ACL для {program}: {e}")
        return False




def main(programs_file: str, restricted_users_file: str):
    """Основная логика скрипта."""
    global printWell
    # Читаем список программ и пользователей

    # main_dir         = os.path.dirname(os.path.abspath(__file__))
    # programs         = read_programs_file(os.path.join(main_dir, programs_file))
    # restricted_users = read_programs_file(os.path.join(main_dir, restricted_users_file))
    programs         = read_programs_file(programs_file)
    restricted_users = read_programs_file(restricted_users_file)

    if not programs or not restricted_users:
        print("Ошибка: файл с программами или пользователями пуст.")
        return

    # Проверяем каждую программу для каждого пользователя
    for program in programs:
        program_path = Path(program)

        # Проверяем существование файла
        if not program_path.exists():
            print(f"Предупреждение: программа не существует: {program}")
            continue

        for user in restricted_users:
            if not check_acl_restriction(program, user):
                # print(f"Обнарушено отсутствие ограничения для {program} (пользователь: {user})")
                # Пытаемся установить запрет
                set_acl_restriction(program, user);
            else:
                if printWell:
                    print(f"Ограничение корректно для {program} (пользователь: {user})")



if __name__ == '__main__':
    # Конфигурация
    PROGRAMS_FILE = 'programs.conf'  # Файл со списком программ
    USERS_FILE = 'users.conf'  # Файл со списком программ

    # Запуск основной логики
    main(PROGRAMS_FILE, USERS_FILE)

