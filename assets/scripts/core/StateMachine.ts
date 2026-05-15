/**
 * StateMachine.ts
 * 通用有限状态机（FSM）实现
 */

export interface IState<T extends string = string> {
    name: T;
    onEnter?: (prevState?: T) => void;
    onUpdate?: (dt: number) => void;
    onExit?: (nextState?: T) => void;
}

export class StateMachine<T extends string = string> {
    private states: Map<T, IState<T>> = new Map();
    private currentState: IState<T> | null = null;
    private previousStateName: T | undefined;

    /** 注册状态 */
    addState(state: IState<T>): this {
        this.states.set(state.name, state);
        return this;
    }

    /** 批量注册状态 */
    addStates(states: IState<T>[]): this {
        states.forEach(s => this.addState(s));
        return this;
    }

    /** 切换状态 */
    transition(nextStateName: T): boolean {
        if (!this.states.has(nextStateName)) {
            console.warn(`[StateMachine] State "${nextStateName}" not found.`);
            return false;
        }

        const prev = this.currentState?.name;

        // 退出当前状态
        if (this.currentState?.onExit) {
            this.currentState.onExit(nextStateName);
        }

        this.previousStateName = prev;
        this.currentState = this.states.get(nextStateName)!;

        // 进入新状态
        if (this.currentState.onEnter) {
            this.currentState.onEnter(prev);
        }

        return true;
    }

    /** 帧更新：驱动当前状态 */
    update(dt: number): void {
        this.currentState?.onUpdate?.(dt);
    }

    /** 获取当前状态名 */
    get current(): T | undefined {
        return this.currentState?.name;
    }

    /** 获取上一个状态名 */
    get previous(): T | undefined {
        return this.previousStateName;
    }

    /** 判断是否处于某状态 */
    is(stateName: T): boolean {
        return this.currentState?.name === stateName;
    }

    /** 重置 FSM */
    reset(): void {
        this.currentState = null;
        this.previousStateName = undefined;
    }
}
