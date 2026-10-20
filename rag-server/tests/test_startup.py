import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from rag.config import Settings
from rag.store import chunk_markdown
from startup import prepare_index


class StartupTests(unittest.TestCase):
	def setUp(self):
		self.directory = tempfile.TemporaryDirectory()
		self.addCleanup(self.directory.cleanup)
		root = Path(self.directory.name)
		self.settings = Settings(
			"127.0.0.1", 8100, root / "chroma", root / "corpus",
			"startup-tests", root / "state" / "technical-key",
		)

	def test_missing_corpus_fails_before_indexing(self):
		with patch("startup.rebuild") as rebuild:
			with self.assertRaisesRegex(RuntimeError, "does not exist"):
				prepare_index(self.settings)
			rebuild.assert_not_called()

	def test_empty_corpus_fails(self):
		self.settings.corpus_dir.mkdir()
		with patch("startup.rebuild", return_value=0):
			with self.assertRaisesRegex(RuntimeError, "no indexable"):
				prepare_index(self.settings)

	def test_failed_ingestion_is_not_hidden(self):
		self.settings.corpus_dir.mkdir()
		with patch("startup.rebuild", side_effect=OSError("disk unavailable")):
			with self.assertRaisesRegex(OSError, "disk unavailable"):
				prepare_index(self.settings)

	def test_unsearchable_general_index_fails(self):
		self.settings.corpus_dir.mkdir()
		with patch("startup.rebuild", return_value=1), patch("startup.query", return_value=[]):
			with self.assertRaisesRegex(RuntimeError, "no searchable"):
				prepare_index(self.settings)

	def test_each_start_rebuilds_and_verifies(self):
		self.settings.corpus_dir.mkdir()
		with patch("startup.rebuild", return_value=3) as rebuild, patch(
			"startup.query", return_value=[object()],
		) as query:
			self.assertEqual(prepare_index(self.settings), 3)
			self.assertEqual(prepare_index(self.settings), 3)
			self.assertEqual(rebuild.call_count, 2)
			self.assertEqual(query.call_count, 2)
		self.assertTrue(self.settings.technical_key_path.parent.is_dir())

	def test_technical_subsections_do_not_become_general(self):
		chunks = chunk_markdown(
			"# Public\nGeneral help.\n## TECHNICAL-storage\nInternal details.\n"
			"### Tables\nInternal tables.\n## Help\nGeneral help again.\n",
		)
		self.assertEqual([chunk.technical for chunk in chunks], [False, True, True, False])


if __name__ == "__main__":
	unittest.main()
