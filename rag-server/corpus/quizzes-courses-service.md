# Quizzes and Courses Service

Learn a language with prepared courses, lessons, quizzes and revision
activities, with AI-generated study help from Garry.

## Courses and lessons

- Browse courses in German, French, Italian, Dutch, Spanish and Polish.
- Work through lessons containing prepared learning content and vocabulary.
- Complete lessons to build your learnt-vocabulary collection.
- Review lesson vocabulary with flashcards.

## Quizzes and progress

- Take quizzes associated with a course and its lessons. Questions can include
  interactive exercises, such as arranging words to build a sentence.
- Review your course progress, including completed lessons and quiz results.
  Progress includes your best score and the number of questions for each quiz.
- See milestones for completed courses, lessons and quizzes.

## Garry and documentation

- Ask Garry to explain the course or lesson you are studying, help with
  vocabulary, suggest a short practice example, or explain how a LanguageWise
  feature works.
- Garry can use course information and your own learning progress to provide
  relevant help. It does not provide quiz questions or answers through its
  course tools.
- Use **Ask the docs** to ask natural-language questions about LanguageWise.
  Garry answers from relevant documentation passages and shows their sources.
  If the documentation does not support an answer, it says so rather than
  guessing.

## For agents

The shared MCP server provides these read-only tools in the `courses` scope.
Personal learning tools act on behalf of the signed-in learner.

- `courses_list_courses`: lists available courses and their codes.
- `courses_list_lessons(courseCode)`: lists lessons in a course.
- `courses_list_quizzes(courseCode)`: lists quizzes in a course without
  exposing their questions or answers.
- `courses_get_lesson_vocabulary(courseCode, lessonSlug)`: returns vocabulary
  taught in a lesson.
- `courses_get_flashcards(courseCode, lessonSlug)`: returns the lesson's
  flashcards.
- `courses_get_my_progress(courseCode)`: returns the learner's progress in a
  course, including lesson completion and quiz results.
- `courses_get_my_vocabulary(courseCode?)`: returns vocabulary the learner has
  unlocked by completing lessons, optionally limited to one course.
- `courses_get_my_milestones(limit?)`: returns recent completed-course,
  lesson and quiz milestones. The optional limit is 1-50 and defaults to 10.

Garry can also use the shared `docs_search` tool to retrieve general
LanguageWise documentation when it needs information that is not already in
the conversation context. Internal technical documentation is not returned by
learner-facing documentation searches.
