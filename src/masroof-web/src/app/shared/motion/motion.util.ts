/**
 * Shared Motion (motion.dev) helpers. The `motion` package is bundled — no CDN,
 * consistent with Masroof's zero-egress premise.
 *
 * Every animation here is a progressive enhancement: if the user prefers reduced
 * motion, elements are shown in their final state with no movement.
 */

/** True when the user has asked the OS to minimise non-essential motion. */
export function prefersReducedMotion(): boolean {
  try {
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  } catch {
    return false;
  }
}

/** The signature easing used across the app — a crisp, engineered ease-out. */
export const EASE_OUT: [number, number, number, number] = [0.16, 1, 0.3, 1];
