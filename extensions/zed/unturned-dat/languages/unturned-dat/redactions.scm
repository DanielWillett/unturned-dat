; https://zed.dev/docs/extensions/languages?highlight=markdown#text-redactions

; Property values
(unquoted_value) @redact
(property_line
  value: (quoted_string
    [
      (quoted_string_segment) @redact
      (ERROR) @redact
      (escape_sequence) @redact
    ]+
  )
)

; List values
(unquoted_list_value) @redact
(list
  (quoted_string
    [
      (quoted_string_segment) @redact
      (ERROR) @redact
      (escape_sequence) @redact
    ]+
  )
)