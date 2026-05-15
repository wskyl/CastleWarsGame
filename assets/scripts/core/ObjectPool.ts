/**
 * ObjectPool.ts
 * 通用对象池 —— 避免频繁 GC，用于单位、特效等高频创建/销毁对象
 */

import { Node, NodePool, instantiate, Prefab } from 'cc';

export class ObjectPool {
    private pool: NodePool;
    private prefab: Prefab;
    private _name: string;
    private activeObjects: Set<Node> = new Set();

    constructor(prefab: Prefab, name: string = 'pool') {
        this.prefab = prefab;
        this._name = name;
        this.pool = new NodePool();
    }

    /**
     * 预热对象池（提前创建若干对象）
     */
    warmUp(count: number, parent: Node): void {
        for (let i = 0; i < count; i++) {
            const node = instantiate(this.prefab);
            node.setParent(parent);
            node.active = false;
            this.pool.put(node);
        }
    }

    /**
     * 获取一个对象（从池中取或新建）
     */
    get(parent: Node): Node {
        let node = this.pool.get();
        if (!node) {
            node = instantiate(this.prefab);
        }
        node.setParent(parent);
        node.active = true;
        this.activeObjects.add(node);
        return node;
    }

    /**
     * 归还对象到池中
     */
    put(node: Node): void {
        if (!node) return;
        node.active = false;
        this.activeObjects.delete(node);
        this.pool.put(node);
    }

    /**
     * 归还所有活跃对象
     */
    returnAll(): void {
        this.activeObjects.forEach(node => {
            if (node && node.isValid) {
                node.active = false;
                this.pool.put(node);
            }
        });
        this.activeObjects.clear();
    }

    /**
     * 清空对象池
     */
    clear(): void {
        this.returnAll();
        this.pool.clear();
    }

    get size(): number {
        return this.pool.size();
    }

    get activeCount(): number {
        return this.activeObjects.size;
    }
}

/** 多类型对象池注册表 */
export class PoolRegistry {
    private pools: Map<string, ObjectPool> = new Map();

    register(key: string, prefab: Prefab): void {
        if (!this.pools.has(key)) {
            this.pools.set(key, new ObjectPool(prefab, key));
        }
    }

    get(key: string): ObjectPool | undefined {
        return this.pools.get(key);
    }

    clearAll(): void {
        this.pools.forEach(pool => pool.clear());
        this.pools.clear();
    }
}

export const GlobalPoolRegistry = new PoolRegistry();
