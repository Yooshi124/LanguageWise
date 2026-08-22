<!--
  SKELETON PROOF OF CONCEPT — not part of the Mini Games feature.

  This component exists only to demonstrate the three-tier wiring for this
  microservice:

      frontend (this file)  ->  backend API  ->  database service  ->  SQLite

  It fetches the placeholder SampleItems table through the nginx /api/ proxy.
  Delete it once the Mini Games feature calls the backend for real.

  Reachable at http://localhost:3001/sample-items
-->
<template>
	<main class="skeleton">
		<h1>Skeleton demo — SampleItems</h1>
		<p class="skeleton__note">
			Served by the frontend container, fetched from the backend API, which reads
			the SQLite database through the database service.
		</p>

		<p v-if="state === 'loading'">Loading…</p>
		<p v-else-if="state === 'error'" class="skeleton__error">{{ error }}</p>

		<table v-else class="skeleton__table">
			<thead>
				<tr><th>Id</th><th>Name</th><th>Description</th><th>Created</th></tr>
			</thead>
			<tbody>
				<tr v-for="item in items" :key="item.id">
					<td>{{ item.id }}</td>
					<td>{{ item.name }}</td>
					<td>{{ item.description }}</td>
					<td>{{ item.createdAt }}</td>
				</tr>
			</tbody>
		</table>

		<p><a href="/">Back to Mini Games</a></p>
	</main>
</template>

<script setup>
import { onMounted, ref } from 'vue';

const items = ref([]);
const error = ref('');
const state = ref('loading');

onMounted(async () => {
	try {
		const response = await fetch('/api/sample-items');
		if (!response.ok) {
			throw new Error(`The backend returned ${response.status}.`);
		}
		items.value = await response.json();
		state.value = 'ready';
	} catch (exception) {
		error.value = exception.message;
		state.value = 'error';
	}
});
</script>

<style scoped>
.skeleton {
	max-width: 60rem;
	margin: 0 auto;
	padding: 2rem;
	font-family: Arial, sans-serif;
}

.skeleton__note {
	color: #555;
}

.skeleton__error {
	color: #b00020;
}

.skeleton__table {
	width: 100%;
	border-collapse: collapse;
}

.skeleton__table th,
.skeleton__table td {
	border: 1px solid #ccc;
	padding: 0.5rem;
	text-align: left;
}

.skeleton__table th {
	background: #f4f4f4;
}
</style>
