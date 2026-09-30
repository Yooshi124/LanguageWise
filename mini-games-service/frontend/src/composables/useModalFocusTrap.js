import { nextTick, ref, watch } from 'vue';

/**
 * Focus-trap behaviour for a modal dialog: moves focus into the dialog when it opens (to
 * `initialFocusRef` if given, otherwise the first focusable element), keeps Tab/Shift+Tab
 * cycling within `containerRef`, restores focus to whatever opened the dialog when it closes,
 * and treats Escape as a request to close.
 *
 * @param {import('vue').Ref<boolean>} visibleRef
 * @param {() => void} onClose
 * @param {import('vue').Ref<HTMLElement|null>} [initialFocusRef]
 */
export function useModalFocusTrap(visibleRef, onClose, initialFocusRef = null) {
	const containerRef = ref(null);
	let previouslyFocusedElement = null;

	function getFocusableElements() {
		if (!containerRef.value) return [];
		return Array.from(containerRef.value.querySelectorAll('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'))
			.filter((element) => !element.disabled);
	}

	function trapFocus(event) {
		if (event.key === 'Escape') {
			event.preventDefault();
			onClose();
			return;
		}
		if (event.key !== 'Tab') return;
		const focusable = getFocusableElements();
		if (focusable.length === 0) return;
		const first = focusable[0];
		const last = focusable[focusable.length - 1];
		if (event.shiftKey && document.activeElement === first) {
			event.preventDefault();
			last.focus();
		} else if (!event.shiftKey && document.activeElement === last) {
			event.preventDefault();
			first.focus();
		}
	}

	watch(visibleRef, (visible) => {
		if (visible) {
			previouslyFocusedElement = document.activeElement;
			nextTick(() => (initialFocusRef?.value ?? getFocusableElements()[0])?.focus());
		} else if (previouslyFocusedElement instanceof HTMLElement) {
			previouslyFocusedElement.focus();
			previouslyFocusedElement = null;
		}
	});

	return { containerRef, trapFocus };
}
