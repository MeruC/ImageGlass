const tsPlugin = require('@typescript-eslint/eslint-plugin');
const tsParser = require('@typescript-eslint/parser');

const isProduction = process.env.NODE_ENV === 'production';

module.exports = [
  {
    ignores: ['node_modules/**', 'dist/**'],
  },
  {
    files: ['src/**/*.ts', 'src/**/*.js'],
    languageOptions: {
      ecmaVersion: 12,
      sourceType: 'module',
      parser: tsParser,
      parserOptions: {
        tsconfigRootDir: __dirname,
        project: './tsconfig.json',
      },
    },
    plugins: {
      '@typescript-eslint': tsPlugin,
    },
    rules: {
      'no-multiple-empty-lines': ['error', {
        max: 2,
        maxEOF: 1,
        maxBOF: 1,
      }],
      '@typescript-eslint/ban-ts-comment': 'off',
      '@typescript-eslint/naming-convention': 'off',
      '@typescript-eslint/no-explicit-any': 'off',
      'arrow-parens': 'off',
      'brace-style': ['error', 'stroustrup', { allowSingleLine: true }],
      camelcase: 'off',
      'class-methods-use-this': 'off',
      'linebreak-style': 'off',
      'lines-between-class-members': 'off',
      'max-classes-per-file': 'off',
      'no-await-in-loop': 'off',
      'no-console': [
        isProduction ? 'error' : 'warn',
        {
          allow: ['info', 'warn', 'error'],
        },
      ],
      'no-continue': 'off',
      'no-debugger': isProduction ? 'error' : 'warn',
      'no-empty': 'off',
      'no-param-reassign': 'off',
      'no-plusplus': 'off',
      'no-restricted-globals': 'off',
      // https://github.com/typescript-eslint/typescript-eslint/blob/main/docs/linting/TROUBLESHOOTING.md#i-get-errors-from-the-no-undef-rule-about-global-variables-not-being-defined-even-though-there-are-no-typescript-errors
      'no-undef': 'off',
      'prefer-object-spread': 'off',
    },
  },
];
