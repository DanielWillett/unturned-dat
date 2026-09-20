(begin_dictionary) @punctuation.bracket.curly
(end_dictionary) @punctuation.bracket.curly
(begin_list) @punctuation.bracket.square @punctuation.list_marker
(end_list) @punctuation.bracket.square @punctuation.list_marker

(comment) @comment.line

(property_line
  key: (quoted_string
    "\"" @punctuation.string
    [
      (quoted_string_segment (#set! "priority" 127)) @property
      (ERROR (#set! "priority" 127)) @property
      (escape_sequence) @string.escape
    ]+
    "\"" @punctuation.string
  )
)

(unquoted_key) @property

(quoted_string "," @punctuation.delimiter)

([(unquoted_value) (unquoted_list_value)] @number.guid
  (#match? @number.guid "^\\s*[0-9a-fA-F]{8}(?:-{0,1}[0-9a-fA-F]{4}){3}-{0,1}[0-9a-fA-F]{12}\\s*$")
  (#set! "priority" 126)
)

(quoted_string
  "\"" @punctuation.string
  ((quoted_string_segment) @number.guid
    (#match? @number.guid "^\\s*[0-9a-fA-F]{8}(?:-{0,1}[0-9a-fA-F]{4}){3}-{0,1}[0-9a-fA-F]{12}\\s*$")
    (#set! "priority" 126)
  )
  "\"" @punctuation.string
)

([(unquoted_value) (unquoted_list_value)] @number
  (#match? @number "^\\s*[-+]{0,1}(?:[\\.\\,]{0,1}\\d{1,3})+\\s*$")
  (#set! "priority" 125)
)

(quoted_string
  "\"" @punctuation.string
  ((quoted_string_segment) @number
    (#match? @number "^\\s*[-+]{0,1}(?:[\\.\\,]{0,1}\\d{1,3})+\\s*$")
    (#set! "priority" 125)
  )
  "\"" @punctuation.string
)

([(unquoted_value) (unquoted_list_value)] @constant.builtin @boolean
  (#match? @constant.builtin "^(?:[ytnf]|\\s*(?:[tT][rR][uU][eE]|[fF][aA][lL][sS][eE])\\s*)$")
  (#set! "priority" 125)
)

(quoted_string
  "\"" @punctuation.string
  ((quoted_string_segment) @constant.builtin @boolean
    (#match? @constant.builtin "^(?:[ytnf]|\\s*(?:[tT][rR][uU][eE]|[fF][aA][lL][sS][eE])\\s*)$")
    (#set! "priority" 125)
  )
  "\"" @punctuation.string
)

([(unquoted_value) (unquoted_list_value)] @variable.special @variable.builtin.this
  (#match? @variable.special "^\\s*[tT][hH][iI][sS]\\s*$")
  (#set! "priority" 125)
)

(quoted_string
  "\"" @punctuation.string
  ((quoted_string_segment) @variable.special @variable.builtin.this
    (#match? @variable.special "^\\s*[tT][hH][iI][sS]\\s*$")
    (#set! "priority" 125)
  )
  "\"" @punctuation.string
)

(quoted_string
  "\"" @punctuation.string
  [
    (quoted_string_segment) @string
    (ERROR (#set! "priority" 110)) @string
    (escape_sequence) @string.escape
  ]*
  "\"" @punctuation.string
)

(unquoted_value) @string
(unquoted_list_value) @string