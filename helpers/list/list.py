#!/usr/bin/env python3
# ln -s /A/_/Linux_Записки/Mint/Prg/helpers/list.py /usr/local/bin/list
# Справка: см. запрос о производстве скрипта list.py.query

import os
import sys
import re
import pwd
import grp
import stat
import math
import argparse
import sys

from datetime import datetime


def create_parser():
    parser = argparse.ArgumentParser(
        prog='list.py',
        description='Программа list.py — вывод директорий и файлов текущего каталога с поддержкой фильтров.',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        add_help=False
    )

    # Справка
    help_group = parser.add_argument_group('Справка')
    help_group.add_argument(
        '-h', '--help',
        action='help',
        help='Вывод справки по использованию программы.'
    )
    
    # Фильтр по дате
    date_group = parser.add_argument_group('Фильтры по дате')
    date_group.add_argument(
        '-date',
        metavar='[DATE,DATE]',
        type=parse_date_range,
        help='Фильтр по диапазону дат изменения файлов. '
              'Примеры: '
              '[12.08.2026,12.08.2027] — между датами, '
              '[,12.08.2027] или [*,12.08.2027] — до даты, '
              '[12.08.2027,] или [12.08.2027,*] — после даты, '
              '[2022,*] — с начала 2022 года и далее, '
              '[02.2022,*] или [2022.02,*] — с февраля 2022 и далее.'
    )

    # Уровень рекурсии
    parser.add_argument(
        '-L',
        type=int,
        default=0,
        help='Уровень рекурсии. 0 и 1 — только текущий каталог. '
             'Положительное n — n уровней вниз. -1 — родитель и вниз. '
             '-a — подъём на a уровней вверх и рекурсивный обход на a уровней вниз.'
    )

    # Вывод размера
    parser.add_argument(
        '-s',
        action='store_true',
        help='Вывести общий размер всех файлов (человеко‑читаемый формат). '
             'Учитывает все поддиректории вне зависимости от -L. '
             'Защита от зацикливания при символических ссылках обязательна.'
    )

    # Фильтры по пользователю
    user_group = parser.add_argument_group('Фильтры по пользователю')
    user_group.add_argument(
        '-u',
        action='append',
        metavar='user',
        help='Выводить только файлы и директории, принадлежащие пользователю user. Может повторяться несколько раз.'
    )
    user_group.add_argument(
        '-!u',
        action='append',
        dest='not_user',
        metavar='user',
        help='Выводить только файлы и директории, НЕ принадлежащие пользователю user. Может повторяться несколько раз.'
    )

    # Фильтры по группе
    group_group = parser.add_argument_group('Фильтры по группе')
    group_group.add_argument(
        '-g',
        action='append',
        metavar='group',
        help='Выводить только файлы и директории, принадлежащие группе group. Может повторяться несколько раз.'
    )
    group_group.add_argument(
        '-!g',
        action='append',
        dest='not_group',
        metavar='group',
        help='Выводить только файлы и директории, НЕ принадлежащие группе group. Может повторяться несколько раз.'
    )

    # Фильтры по типу объектов
    type_group = parser.add_argument_group('Фильтры по типу объектов')
    type_group.add_argument(
        '-d',
        action='store_true',
        help='Выводить только директории.'
    )
    type_group.add_argument(
        '-f',
        action='store_true',
        help='Выводить только регулярные файлы.'
    )
    type_group.add_argument(
        '-ln',
        action='store_true',
        help='Выводить только символические ссылки.'
    )
    type_group.add_argument(
        '-rln',
        action='store_true',
        help='Рекурсивный обход символических ссылок. Защита от зацикливания обязательна.'
    )

    # Фильтры по имени и содержимому
    search_group = parser.add_argument_group('Фильтры поиска')
    search_group.add_argument(
        '-name',
        metavar='"regex"',
        help='Имя файла соответствует регулярному выражению regex (кавычки обязательны).'
    )
    search_group.add_argument(
        '-content',
        metavar='"regex"',
        help='Хотя бы одна строка файла соответствует регулярному выражению regex. '
              'Найденная строка выводится после имени файла на новой строке (кавычки обязательны).'
    )

    # Фильтр по размеру
    parser.add_argument(
        '-size',
        metavar='"[mi,ma]"',
        help='Фильтр по размеру файла. Формат: [mi,ma], [,ma], [*,ma], [mi], [mi,*], [mi,]. '
              'mi и ma — степени двойки (2^mi и 2^ma байтов).'
    )

    return parser

def human_readable_size(size_bytes):
    """Преобразует размер в байтах в человеко‑читаемый формат."""
    if size_bytes == 0:
        return "   0   Б "

    size_names = ["Б", "КиБ", "МиБ", "ГиБ", "ТиБ", "ПиБ"]
    i = int(math.floor(math.log(size_bytes, 1024)))
    p = math.pow(1024, i)
    s = int(round(size_bytes / p, 2))

    return f"{s:>4} {size_names[i]:>3} "

def matches_size_filter(file_size, size_filter):
    """Проверяет, соответствует ли размер файла фильтру -size."""
    if not size_filter:
        return True

    # Парсим формат [mi,ma], [,ma], [*,ma], [mi], [mi,*], [mi,]
    cleaned = size_filter.strip("[] ")
    parts = cleaned.split(',')

    min_size = None
    max_size = None

    if len(parts) == 1:
        part = parts[0].strip()
        if part.endswith('*'):
            min_size = int(part[:-1])
        elif part.startswith('*'):
            max_size = int(part[1:])
        else:
            min_size = int(part)
    else:
        min_part, max_part = parts[0].strip(), parts[1].strip()
        if min_part and min_part != '*':
            min_size = int(min_part)
        if max_part and max_part != '*':
            max_size = int(max_part)

    min_bytes = 2 ** min_size if min_size is not None else 0
    max_bytes = 2 ** max_size if max_size is not None else float('inf')

    return min_bytes <= file_size <= max_bytes

def should_include_by_user_group(path, args):
    """Проверяет, должен ли файл/директория быть включён по фильтрам -u, -!u, -g, -!g."""
    try:
        stat_info = os.stat(path)
        uid = stat_info.st_uid
        gid = stat_info.st_gid

        # Проверка пользователя
        if args.u:
            if pwd.getpwuid(uid).pw_name not in args.u:
                return False
        if args.not_user:
            if pwd.getpwuid(uid).pw_name in args.not_user:
                return False

        # Проверка группы
        if args.g:
            if grp.getgrgid(gid).gr_name not in args.g:
                return False
        if args.not_group:
            if grp.getgrgid(gid).gr_name in args.not_group:
                return False
            
    except (KeyError, OSError):
        #! Тихая обработка исключений.
        return False
    
    return True

def matches_name_filter(filename, name_pattern):
    """Проверяет соответствие имени файла регулярному выражению."""
    if not name_pattern:
        return True
    
    return re.search(name_pattern, filename) is not None

def matches_content_filter(filepath, content_pattern):
    """Проверяет содержимое файла на соответствие регулярному выражению."""
    if not content_pattern:
        return True, None
    
    try:
        with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
            for line in f:
                if re.search(content_pattern, line):
                    return True, line.rstrip()
                
    except (OSError, UnicodeDecodeError):
        #! Тихая обработка исключений.
        pass
    
    return False, None


def parse_date(date_str):
    """
    Парсит дату из строки в различных форматах.
    Возвращает объект datetime или None, если строка пустая/звёздочка.
    """
    date_str = date_str.strip()
    if not date_str or date_str == '*':
        return None

    # Формат: ДД.ММ.ГГГГ (12.08.2026)
    match = re.match(r'^(\d{1,2})\.(\d{1,2})\.(\d{4})$', date_str)
    if match:
        day, month, year = map(int, match.groups())
        try:
            return datetime(year, month, day)
        except ValueError as e:
            raise argparse.ArgumentTypeError(f"Некорректная дата: {date_str}") from e

    # Формат: ММ.ГГГГ (02.2022) или ГГГГ.ММ (2022.02)
    match = re.match(r'^(\d{1,2})\.(\d{4})$', date_str)  # ММ.ГГГГ
    if match:
        month, year = map(int, match.groups())
        try:
            return datetime(year, month, 1)  # День по умолчанию — 1
        except ValueError as e:
            raise argparse.ArgumentTypeError(f"Некорректная дата: {date_str}") from e

    match = re.match(r'^(\d{4})\.(\d{1,2})$', date_str)  # ГГГГ.ММ
    if match:
        year, month = map(int, match.groups())
        try:
            return datetime(year, month, 1)  # День по умолчанию — 1
        except ValueError as e:
            raise argparse.ArgumentTypeError(f"Некорректная дата: {date_str}") from e

    # Формат: ГГГГ (2022)
    match = re.match(r'^(\d{4})$', date_str)
    if match:
        year = int(match.group(1))
        try:
            return datetime(year, 1, 1)  # Месяц и день по умолчанию — 1
        except ValueError as e:
            raise argparse.ArgumentTypeError(f"Некорректная дата: {date_str}") from e

    raise argparse.ArgumentTypeError(
        f"Неподдерживаемый формат даты: '{date_str}'. "
        "Поддерживаемые форматы: ДД.ММ.ГГГГ, ММ.ГГГГ, ГГГГ.ММ, ГГГГ"
    )

def parse_date_range(date_range_str):
    """
    Парсит строку с диапазоном дат в различных форматах.
    Возвращает кортеж (start_date, end_date), где None означает отсутствие ограничения.
    """
    # Убираем пробелы и квадратные скобки
    cleaned = date_range_str.strip().strip('[]')
    parts = cleaned.split(',', maxsplit=1)  # Разделяем максимум на 2 части

    start_date = None
    end_date = None

    # Обрабатываем первую часть (начало диапазона)
    if parts[0]:
        start_date = parse_date(parts[0])

    # Обрабатываем вторую часть (конец диапазона)
    if len(parts) > 1 and parts[1]:
        end_date = parse_date(parts[1])

    return start_date, end_date


def is_file_in_date_range(filepath, start, end):
    """
    Проверяет, лежит ли диапазон последнего изменения и/или доступа к файлу
    в заданном диапазоне дат.

    Параметры:
    - filepath (str): путь к файлу.
    - start (datetime or None): начальная дата диапазона (включительно).
      Если None — нет ограничения снизу.
    - end (datetime or None): конечная дата диапазона (включительно).
      Если None — нет ограничения сверху.

    Возвращает:
    - bool: True, если файл соответствует диапазону дат, иначе False.
    """
    try:
        stat_info = os.stat(filepath)

        # Получаем время последнего изменения (mtime) и доступа (atime)
        mtime = datetime.fromtimestamp(stat_info.st_mtime)
        atime = datetime.fromtimestamp(stat_info.st_atime)

        # Проверяем, попадает ли хотя бы одно из времён (mtime или atime) в диапазон
        matches_mtime = True
        matches_atime = True

        # Проверка для времени изменения (mtime)
        if start is not None:
            if mtime < start:
                matches_mtime = False
        if end is not None:
            if mtime > end:
                matches_mtime = False

        # Проверка для времени доступа (atime)
        if start is not None:
            if atime < start:
                matches_atime = False
        if end is not None:
            if atime > end:
                matches_atime = False

        # Файл подходит, если хотя бы одно время (mtime ИЛИ atime) попадает в диапазон
        return matches_mtime or matches_atime

    except (OSError, FileNotFoundError) as e:
        #! Тихая обработка исключений.
        # Если файл недоступен или не найден, считаем, что не подходит
        # print(f"Ошибка доступа к файлу {filepath}: {e}", file=sys.stderr)
        return False
    except Exception as e:
        #! Тихая обработка исключений.
        # Любые другие непредвиденные ошибки
        # print(f"Неожиданная ошибка при проверке файла {filepath}: {e}", file=sys.stderr)
        return False


def list_files_and_dirs(start_path, level, args, current_depth=0, base_path=None):
    """Рекурсивно выводит файлы и директории с учётом фильтров и уровня рекурсии."""
    if base_path is None:
        base_path = start_path

    try:
        entries = os.scandir(start_path)
    except PermissionError:
        print(f"Permission denied: {start_path}", file=sys.stderr)
        return

    # Собираем все подходящие элементы в список для последующей сортировки
    output_items = []
    TL = 0

    for entry in entries:
        entry_path = entry.path
        rel_path = os.path.relpath(entry_path, base_path)

        # Применяем фильтры
        if not should_include_by_user_group(entry_path, args):
            continue
        if not matches_name_filter(entry.name, args.name):
            continue

        is_dir = entry.is_dir(follow_symlinks=False)
        is_dirL = entry.is_dir(follow_symlinks=True)
        is_file = entry.is_file(follow_symlinks=False)
        is_fileL = entry.is_file(follow_symlinks=True)
        is_link = entry.is_symlink()

        # Фильтрация по типу
        if args.d or args.f or args.ln:
            need = False
            if args.d and is_dir:
                need = True
            elif args.f and is_file:
                need = True
            elif args.ln and is_link:
                need = True

            if not need:
                continue

        if args.date:
            start, end = args.date
            if not is_file_in_date_range(entry_path, start, end):
                continue

        dir_size = 0
        if args.size or args.s:
            if is_dirL:
                dir_size = get_directory_size(entry_path, args.rln, level, current_depth + 1)
            else:
                dir_size = entry.stat().st_size

        # Проверяем размер, если нужно
        if args.size:
            if is_file and not matches_size_filter(entry.stat().st_size, args.size):
                continue
            if is_dir and args.s:
                if not matches_size_filter(dir_size, args.size):
                    continue

        TL += dir_size

        # Формируем строку вывода
        if is_dir or is_dirL:
            output_line = f"{rel_path}/"
        else:
            output_line = rel_path

        # Добавляем размер, если запрошено
        if args.s:
            if is_fileL:
                size_str = human_readable_size(dir_size)
            else:
                size_str = human_readable_size(dir_size)
            output_line = f"{size_str} {output_line}"

        # Поиск содержимого, если нужно
        if args.content and is_fileL:
            found, matched_line = matches_content_filter(entry_path, args.content)
            if found:
                output_items.append((output_line, matched_line, dir_size, is_dirL))
            else:
                continue
        else:
            output_items.append((output_line, None, dir_size, is_dirL))

        # Рекурсивный обход для сбора данных (но не вывод)
        if (is_dir or (is_dirL and args.rln)) and current_depth < level:
            list_files_and_dirs(
                entry_path,
                level,
                args,
                current_depth + 1,
                base_path
            )

    # Сортируем элементы в зависимости от args.s or args.size
    if args.s or args.size:
        # Сортировка по размеру (от меньшего к большему)
        output_items.sort(key=lambda x: x[2])
    else:
        # Сортировка: сначала не-dirL, затем dirL (dirL — последними)
        output_items.sort(key=lambda x: x[3])

    # Выводим отсортированные элементы
    for output_line, matched_content, _, _ in output_items:
        print(output_line)
        if matched_content:
            print(matched_content)

    if level == 0 and TL > 0:
        print(f"{human_readable_size(TL)} .")



def get_directory_size(path, follow_symlinks, max_depth, current_depth=0, visited_links=None):
    """Вычисляет размер директории с защитой от зацикливания через символические ссылки."""
    if visited_links is None:
        visited_links = set()

    total_size = 0

    try:
        entries = os.scandir(path)
    except (PermissionError, FileNotFoundError):
        return 0

    for entry in entries:
        entry_path = entry.path

        try:
            if entry.is_symlink():
                # Защита от зацикливания
                real_path = os.path.realpath(entry_path)
                if real_path in visited_links:
                    continue
                
                visited_links.add(real_path)

                if follow_symlinks and current_depth < max_depth:
                    # Рекурсивно обходим символическую ссылку, если разрешено
                    total_size += get_directory_size(
                        real_path,
                        follow_symlinks and current_depth+1 < max_depth,
                        max_depth,
                        current_depth + 1,
                        visited_links.copy()
                    )
                else:
                    # Считаем размер самой ссылки
                    total_size += os.lstat(entry_path).st_size
                
                continue

                
            #! get_directory_size вызывается на всю глубину рекурсии из каждой директории
            if entry.is_file(follow_symlinks=follow_symlinks):
                total_size += entry.stat(follow_symlinks=follow_symlinks).st_size
                
            elif entry.is_dir(follow_symlinks=follow_symlinks and current_depth < max_depth):
                total_size += get_directory_size(
                    entry_path,
                    follow_symlinks and current_depth+1 < max_depth,
                    max_depth,
                    current_depth + 1,
                    visited_links
                )
        except (OSError, PermissionError):
            #! Тихая обработка исключений.
            continue

    return total_size


def resolve_start_path_and_level(args):
    """Определяет стартовый путь и уровень рекурсии на основе аргумента -L."""
    L = args.L

    if L >= 0:
        # Положительное n или 0/1 — текущий каталог
        start_path = os.getcwd()
        level = L  # 0 и 1 означают один уровень
    else:
        if L == -1:
            # -1 — родительский каталог и вниз
            start_path = os.path.dirname(os.getcwd())
            level = 1  # родитель + текущий
        else:
            # -a — подъём на a уровней вверх
            a = abs(L)
            start_path = os.getcwd()
            for _ in range(a):
                nsp = os.path.dirname(start_path)
                if nsp == start_path:
                    break

                start_path = nsp
                level += 1  # подъём + обход вниз на a уровней

    return start_path, level

def main():
    parser = create_parser()
    args = parser.parse_args()

    # Определяем стартовый путь и уровень рекурсии
    start_path, level = resolve_start_path_and_level(args)

    # Выводим распаршенные аргументы для отладки
    # print(f"Распаршенные аргументы: {vars(args)}")

    # Запускаем обход файлов и директорий
    list_files_and_dirs(start_path, level, args, base_path=start_path)


if __name__ == '__main__':
    main()

