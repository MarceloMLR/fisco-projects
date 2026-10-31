// jsdom has no animations: this stand-in records each one the screens play (ADR-0133), on which element, with which
// frames and options, and ends it at once or when the test says.

export interface Played {
  element: Element;
  frames: Keyframe[];
  options: KeyframeAnimationOptions;
  /** Ends the animation, for one held until the test says. */
  end: () => void;
}

export interface SimulatedAnimations {
  played: Played[];
  /** Ends every animation still running. */
  endAll: () => void;
}

/** Plays the animations of the screens on a stand-in; held, each one runs until the test ends it. */
export function simulateAnimations({ held = false }: { held?: boolean } = {}): SimulatedAnimations {
  const played: Played[] = [];
  HTMLElement.prototype.animate = function animate(
    this: HTMLElement,
    frames: Keyframe[] | PropertyIndexedKeyframes | null,
    options?: number | KeyframeAnimationOptions,
  ) {
    let end: () => void = () => undefined;
    const finished = held
      ? new Promise<void>((resolve) => {
          end = resolve;
        })
      : Promise.resolve();
    played.push({
      element: this,
      frames: frames as Keyframe[],
      options: options as KeyframeAnimationOptions,
      end,
    });
    return { finished, cancel: () => undefined } as unknown as Animation;
  };
  HTMLElement.prototype.getAnimations = () => [];
  return {
    played,
    endAll: () => {
      for (const animation of played) {
        animation.end();
      }
    },
  };
}

/** Back to jsdom without animations. */
export function stopSimulatingAnimations(): void {
  Reflect.deleteProperty(HTMLElement.prototype, 'animate');
  Reflect.deleteProperty(HTMLElement.prototype, 'getAnimations');
}
