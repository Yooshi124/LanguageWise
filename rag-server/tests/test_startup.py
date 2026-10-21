import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import chromadb
from chromadb.utils.embedding_functions import DefaultEmbeddingFunction

from rag.config import Settings
from rag.store import _SingleThreadedDefaultEmbedding, _SingleThreadedOnnxEmbedding, chunk_markdown, get_collection, rebuild
from startup import prepare_index


class StartupTests(unittest.TestCase):
	def test_embedding_session_bounds_native_threads_and_reuses_model(self):
		embedding = _SingleThreadedOnnxEmbedding(preferred_providers=["CPUExecutionProvider"])
		with patch.object(embedding.ort, "InferenceSession") as session:
			self.assertIs(embedding.model, session.return_value)
			self.assertIs(embedding.model, session.return_value)
			session.assert_called_once()
			options = session.call_args.kwargs["sess_options"]
			self.assertEqual(options.intra_op_num_threads, 1)
			self.assertEqual(options.inter_op_num_threads, 1)
			self.assertEqual(session.call_args.kwargs["providers"], ["CPUExecutionProvider"])

	def test_ingestion_and_query_reopen_use_the_same_bounded_embedding(self):
		self.settings.corpus_dir.mkdir()
		with patch("rag.store.chromadb.PersistentClient") as client:
			get_collection(self.settings)
			self.assertEqual(rebuild(self.settings), 0)
			calls = client.return_value.get_or_create_collection.call_args_list
			self.assertEqual(len(calls), 2)
			for call in calls:
				embedding = call.kwargs["embedding_function"]
				self.assertIsInstance(embedding, _SingleThreadedDefaultEmbedding)
				self.assertEqual(embedding.name(), DefaultEmbeddingFunction.name())
				self.assertEqual(embedding.get_config(), DefaultEmbeddingFunction().get_config())
				self.assertIsInstance(embedding._engine, _SingleThreadedOnnxEmbedding)

	def test_original_default_collection_reopens_without_reindexing(self):
		client = chromadb.PersistentClient(path=str(self.settings.chroma_dir))
		original = client.get_or_create_collection(
			name=self.settings.collection, metadata={"hnsw:space": "cosine"},
		)
		reopened = get_collection(self.settings)
		self.assertEqual(original.id, reopened.id)

	def test_collection_query_uses_bounded_engine_instead_of_reconstructed_default(self):
		collection = get_collection(self.settings)
		with patch.object(DefaultEmbeddingFunction, "__call__", side_effect=AssertionError("default bypass")), patch.object(
			_SingleThreadedOnnxEmbedding, "__call__", return_value=[[0.1] * 384],
		) as engine:
			collection.query(query_texts=["LanguageWise documentation"], n_results=1)
			engine.assert_called_once()

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
