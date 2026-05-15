/**
 * EventBus.ts
 * 全局事件总线 —— 各模块间通信中枢
 * 基于 Cocos Creator 3.x EventTarget 封装
 */

import { EventTarget } from 'cc';

type EventHandler<T = any> = (data: T) => void;

class EventBusClass {
    private emitter: EventTarget = new EventTarget();
    private listenerMap: Map<string, Set<Function>> = new Map();

    /**
     * 订阅事件
     * @param eventName 事件名
     * @param handler 处理函数
     * @param target 事件上下文（用于自动解绑）
     */
    on<T = any>(eventName: string, handler: EventHandler<T>, target?: any): void {
        this.emitter.on(eventName, handler, target);
        if (!this.listenerMap.has(eventName)) {
            this.listenerMap.set(eventName, new Set());
        }
        this.listenerMap.get(eventName)!.add(handler);
    }

    /**
     * 订阅事件（只触发一次）
     */
    once<T = any>(eventName: string, handler: EventHandler<T>, target?: any): void {
        this.emitter.once(eventName, handler, target);
    }

    /**
     * 取消订阅事件
     */
    off(eventName: string, handler?: Function, target?: any): void {
        this.emitter.off(eventName, handler as any, target);
        if (handler && this.listenerMap.has(eventName)) {
            this.listenerMap.get(eventName)!.delete(handler);
        }
    }

    /**
     * 发射事件
     */
    emit<T = any>(eventName: string, data?: T): void {
        this.emitter.emit(eventName, data);
    }

    /**
     * 移除目标对象所有监听
     */
    targetOff(target: any): void {
        this.emitter.targetOff(target);
    }

    /**
     * 清除所有监听器
     */
    clear(): void {
        this.listenerMap.forEach((handlers, eventName) => {
            handlers.forEach(handler => {
                this.emitter.off(eventName, handler as any);
            });
        });
        this.listenerMap.clear();
    }

    /**
     * 获取某事件的监听数量（调试用）
     */
    listenerCount(eventName: string): number {
        return this.listenerMap.get(eventName)?.size ?? 0;
    }
}

// 单例导出
export const EventBus = new EventBusClass();
