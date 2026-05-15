/**
 * ObjectPool.ts
 * 通用对象池 —— 避免频繁 GC，用于单位、特效等高频创建/销毁对象
 * CC 3.8.5 适配：移除已废弃的 NodePool，改用数组实现
 */

import { Node, instantiate, Prefab } from 'cc';

export class ObjectPool {
    private _pool: Node[] = [];
    private _prefab: Prefab;
    private _name: string;
    private _activeObjects: Set<Node> = new Set();

    constructor(prefab: Prefab, name: string = 'pool') {
        this._prefab = prefab;
        this._name = name;
    }

    /**
     * 预热对象池（提前创建若干对象备用）
     */
    warmUp(count: number, parent: Node): void {
        for (let i = 0; i < count; i++) {
            const node = instantiate(this._prefab);
            node.setParent(parent);
            node.active = false;
            this._pool.push(node);
        }
    }

    /**
     * 从池中取出一个节点（池空则新建）
     */
    get(parent: Node): Node {
        let node: Node;
        if (this._pool.length > 0) {
            node = this._pool.pop()!;
        } else {
            node = instantiate(this._prefab);
        }
        node.setParent(parent);
        node.active = true;
        this._activeObjects.add(node);
        return node;
    }

    /**
     * 归还节点到池中
     */
    put(node: Node): void {
        if (!node || !node.isValid) return;
        node.active = false;
        this._activeObjects.delete(node);
        this._pool.push(node);
    }

    /**
     * 将所有活跃对象归还到池中
     */
    returnAll(): void {
        this._activeObjects.forEach(node => {
            if (node && node.isValid) {
                node.active = false;
                this._pool.push(node);
            }
        });
        this._activeObjects.clear();
    }

    /**
     * 清空对象池并销毁所有节点（场景切换时调用）
     */
    clear(): void {
        this.returnAll();
        for (const node of this._pool) {
            if (node && node.isValid) {
                node.destroy();
            }
        }
        this._pool = [];
    }

    /** 池中待用节点数量 */
    get size(): number {
        return this._pool.length;
    }

    /** 当前活跃节点数量 */
    get activeCount(): number {
        return this._activeObjects.size;
    }
}

/** 多类型对象池注册表 */
export class PoolRegistry {
    private _pools: Map<string, ObjectPool> = new Map();

    register(key: string, prefab: Prefab): void {
        if (!this._pools.has(key)) {
            this._pools.set(key, new ObjectPool(prefab, key));
        }
    }

    get(key: string): ObjectPool | undefined {
        return this._pools.get(key);
    }

    clearAll(): void {
        this._pools.forEach(pool => pool.clear());
        this._pools.clear();
    }
}

export const GlobalPoolRegistry = new PoolRegistry();
