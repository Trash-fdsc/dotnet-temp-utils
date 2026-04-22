#!/bin/bash
# ln -s /Arcs/Repos/smalls/dotnet-temp-utils/helpers/shorts/toarc.sh /usr/local/bin/toarc
# toarc dir_name [group_name]
# Напиши скрипт на bash, который делает следующее:
# 1. Получает имя папки dir_name.
# 2. Добавляет к имени папки ".7z" - это имя файла архива.
# 3. Создаёт архив с помощью 7z a -t7z -stl -mx=9 -m0=LZMA2 -md=64m -ms=on -mmt=on -ssc -ssw arc_name dir_name
# 4. Если установлен второй параметр, то это имя group. Вызывает chmod :group, а если параметр не установлен, то chmod :arcs-read для данного файла (меняет группу файла).
# 5. Убирает с файла (архива) разрешения на запись.
# 6. Вызывает для файла команду "sudo chattr +iA arc_name".
# 7. Тестирует архив на корректность.

# Проверка наличия первого аргумента (имя папки)
if [ -z "$1" ]; then
    echo "Ошибка: не указано имя папки." >&2
    echo "Использование: $0 <dir_name> [group]" >&2
    exit 1
fi

DIR_NAME="$1"
ARC_NAME="${DIR_NAME}.7z"

# Проверка существования папки
if [ ! -d "$DIR_NAME" ]; then
    echo "Ошибка: папка '$DIR_NAME' не существует." >&2
    exit 1
fi

# Проверка, существует ли уже архив с таким именем
if [ -f "$ARC_NAME" ]; then
    echo "Ошибка: архив '$ARC_NAME' уже существует." >&2
    exit 2
fi

# Создание архива
echo "К обработке " $(find "$ARC_NAME" -type f | wc -l) " файлов"
echo "Создаю архив '$ARC_NAME' из папки '$DIR_NAME'..."
7z a -t7z -stl -mx=9 -m0=LZMA2 -md=512m -ms=on -mmt=on -ssc -ssw -bb0 -bd -bsp0 -bso0 -bse2 "$ARC_NAME" "$DIR_NAME"

echo
echo

if [ $? -ne 0 ]; then
    echo "Ошибка: создание архива завершилось неудачно." >&2
    exit 4
fi

echo "Архив успешно создан."

# Установка группы файла
if [ -n "$2" ]; then
    GROUP="$2"
else
    GROUP="arcs-read"
fi

echo "Меняю группу файла на '$GROUP'..."
chgrp "$GROUP" "$ARC_NAME"

if [ $? -ne 0 ]; then
    echo "Ошибка: не удалось изменить группу файла." >&2
    exit 5
fi

# Убираем разрешения на запись
echo "Убираю разрешения на запись для файла '$ARC_NAME'..."
chmod a-w "$ARC_NAME"

if [ $? -ne 0 ]; then
    echo "Ошибка: не удалось убрать разрешения на запись." >&2
    exit 6
fi

# Устанавливаем атрибуты с помощью chattr
echo "Устанавливаю атрибуты 'iA' для файла '$ARC_NAME'..."
sudo chattr +iA -- "$ARC_NAME"

if [ $? -ne 0 ]; then
    echo "Ошибка: не удалось установить атрибуты файла." >&2
    exit 7
fi

# Тестирование архива на корректность
echo "Тестирую архив '$ARC_NAME' на корректность..."
7z t -slt "$ARC_NAME"

if [ $? -ne 0 ]; then
    echo "Ошибка: тестирование архива выявило проблемы." >&2
    exit 8
fi

echo "Все операции успешно завершены. Архив '$ARC_NAME' готов и защищён."
