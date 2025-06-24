import { nodeResolve } from '@rollup/plugin-node-resolve';
import alias from '@rollup/plugin-alias';
import path from 'path';

export default {
  input: 'dist/worker.js',
  output: {
    file: 'dist/bundle.js',
    format: 'iife',
    name: 'CloudflareWorker'
  },
  plugins: [
    alias({
      entries: [
        { find: /^\.\.\/fable_modules\/(.*)$/, replacement: path.resolve('dist/fable_modules/$1') }
      ]
    }),
    nodeResolve({
      preferBuiltins: false
    })
  ]
}; 