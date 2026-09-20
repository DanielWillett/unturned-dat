; https://zed.dev/docs/extensions/languages?highlight=markdown#code-outlinestructure

(property_line
  [ key: (quoted_string (quoted_string_segment) @name) key: (unquoted_key) @name ]
  [
      value: (quoted_string (quoted_string_segment) @context)
      value: (unquoted_value) @context
      value: (dictionary (begin_dictionary) @context (end_dictionary) @context)
      value: (list (begin_list) @context (end_list) @context)
  ]?
) @item

(list
    (dictionary (begin_dictionary) @name (end_dictionary) @name) @item
)

(list
    [
        (quoted_string (quoted_string_segment) @name) @item
        (unquoted_list_value) @name @item
    ]
)