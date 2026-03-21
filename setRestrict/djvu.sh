#!/bin/bash
# sudo ln -s /Arcs/Repos/smalls/dotnet-temp-utils/setRestrict/djvu.sh /usr/local/bin/djvu

# Запускаем программу от имени пользователя 'atril' с оставшимися параметрами
sudo -u xreader /usr/bin/xreader "$@"
