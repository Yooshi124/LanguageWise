{{CONTEXT}}

SOURCE CODE
{{SOURCE_CODE}}

REVIEW REQUEST
{{USER_PROMPT}}

Task:
Review the source code above against the review request. Identify every distinct,
evidence-backed problem you can support with the supplied code.

- Order findings by severity, highest first.
- Return at most {{MAX_FINDINGS}} findings.
- If the code is sound with respect to the request, return an empty findings list
  and explain in `summary` why no evidence-backed issue was found.
