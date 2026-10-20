/**
 * The six relationship arrows of a UML class diagram (spec 008 §3.5): each drawn by Mermaid from `diagram` (so it
 * looks exactly like the arrows in lessons), its name, what it means and the C# that makes it.
 */
export interface LegendArrow {
  name: string;
  diagram: string;
  meaning: string;
  code: string;
}

export const LEGEND_ARROWS: LegendArrow[] = [
  {
    name: 'Association',
    diagram: 'Order --> Customer',
    meaning: 'knows: holds a reference',
    code: 'class Order { Customer customer; }',
  },
  {
    name: 'Aggregation',
    diagram: 'Team o-- Player',
    meaning: 'has: the part lives on its own',
    code: 'class Team { List<Player> players; }',
  },
  {
    name: 'Composition',
    diagram: 'Order *-- OrderLine',
    meaning: 'is made of: parts die with it',
    code: 'class Order { List<OrderLine> lines = new(); }',
  },
  {
    name: 'Inheritance',
    diagram: 'Shape <|-- Circle',
    meaning: 'is a',
    code: 'class Circle : Shape',
  },
  {
    name: 'Realization',
    diagram: 'IShape <|.. Square',
    meaning: 'implements: can do',
    code: 'class Square : IShape',
  },
  {
    name: 'Dependency',
    diagram: 'Report ..> Printer',
    meaning: 'uses for a moment',
    code: 'void Print(Printer printer)',
  },
];

/** One line to remember them by. */
export const LEGEND_HINT = 'Solid line: holds it. Dashed: uses or implements. Triangle: is a. Diamond: owns.';

export function legendSource(arrow: LegendArrow): string {
  return `classDiagram\n  direction LR\n  ${arrow.diagram}`;
}
