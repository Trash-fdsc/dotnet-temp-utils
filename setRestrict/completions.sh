#!/bin/bash
# Даём регистрацию автодополнений для вызовов сокращённых скриптов в .bashrc
# bash /Arcs/Repos/smalls/dotnet-temp-utils/setRestrict/completions.sh

_files_completion()
{
    local cur prev
    COMPREPLY=()

    cur="${COMP_WORDS[COMP_CWORD]}"
    prev="${COMP_WORDS[COMP_CWORD-1]}"

    if [[ $COMP_CWORD -eq 1 ]]; then
        local files search_pattern=()
        local ext

        # Формируем шаблон поиска из переданных расширений
        for ext in "$@"; do
            if [[ -z "$search_pattern" ]]; then
                search_pattern+=("-name" "*.$ext")
            else
                search_pattern+=("-o" "-name" "*.$ext")
            fi
        done

        # search_pattern=("-name" "*.pdf" "-o" "-name" "*.epub")
        # echo ${search_pattern[@]}
        # echo ${#files[@]}
        if [[ -n "$search_pattern" ]]; then
            mapfile -t files < <(find . -type f ${search_pattern[@]} 2>/dev/null | sed 's|^\./||' | sed 's|^|"|g' | sed 's|$|"|g')
        fi

        # Фильтруем вручную
        for file in "${files[@]}"; do
            if [[ "$file" == "$cur"* ]]; then
                COMPREPLY+=("$file")
            fi
        done

    else
        COMPREPLY=()
    fi
}

_pdf_files_completion()
{
   _files_completion pdf
}
complete -F _pdf_files_completion pdf

_doc_files_completion()
{
   _files_completion doc docx
}
complete -F _doc_files_completion doc

_djvu_files_completion()
{
   _files_completion djvu
}
complete -F _djvu_files_completion djvu

_epub_files_completion()
{
   _files_completion epub
}
complete -F _epub_files_completion epub

_img_files_completion()
{
   _files_completion jpg jpeg jpe gif tif tiff webp png bmp ico svg pcx wmf emf
}
complete -F _img_files_completion img
complete -F _img_files_completion imgw

