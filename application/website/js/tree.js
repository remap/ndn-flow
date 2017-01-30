/************************************************
 * D3 sync tree render function
 ************************************************/
// var tree = [{
//   "name": "/",
//   "children": []
// }]
var treeData = [
  {
    "name": "/",
    "parent": "null",
    "children": []
  }
];

// ************** Generate the tree diagram  *****************
var width_total = 4000;
var height_total = 600;

var margin = {top: 20, right: 120, bottom: 20, left: 120},
  width = width_total - margin.right - margin.left,
  height = height_total - margin.top - margin.bottom;
  
var i = 0,
  duration = 750,
  root;

var cutOffLength = 10;

var tree = d3.layout.tree()
  .size([height, width]);

var diagonal = d3.svg.diagonal()
  .projection(function(d) { return [d.y, d.x]; });

var svg = d3.select("#tree").append("svg")
  .attr("viewbox", "0, 0, " + width_total + ", " + height_total)
  .attr("width", width + margin.right + margin.left)
  .attr("height", height + margin.top + margin.bottom)
  .append("g")
  .attr("transform", "translate(" + margin.left + "," + margin.top + ")");

root = treeData[0];
root.x0 = height / 2;
root.y0 = 0;
  
update(root);

d3.select(self.frameElement).style("height", "500px");

function update(source) {

  // Compute the new tree layout.
  var nodes = tree.nodes(root).reverse(),
      links = tree.links(nodes);

  // Normalize for fixed-depth.
  nodes.forEach(function(d) { d.y = d.depth * 120; });
  // contentNodes.forEach(function(d) { d.y = d.depth * 120; });

  // Update the nodes
  var node = svg.selectAll("g.node")
    .data(nodes, function(d) { return d.id || (d.id = ++i); });

  // Enter any new nodes at the parent's previous position.
  var nodeEnter = node.enter().append("g")
    .attr("class", "node")
    .attr("transform", function(d) { return "translate(" + source.y0 + "," + source.x0 + ")"; })
    .on("click", click);
  
  nodeEnter.append("circle")
    .attr("r", 1e-6)
    .style("fill", function(d) { return d._children ? "lightsteelblue" : "#fff"; });

  nodeEnter.append("text")
    .attr("x", function(d) { return d.children || d._children ? 0 : 0; })
    .attr("dy", "-1em")
    .attr("text-anchor", function(d) { return d.children || d._children ? "end" : "start"; })
    .text(function(d) {
      if (d.name.length <= cutOffLength || d.is_content === true) {
        return d.name;
      } else {
        return d.name.substring(0, cutOffLength);
      }
    })
    .style("fill-opacity", 1e-6);

  // Transition nodes to their new position.
  var nodeUpdate = node.transition()
    .duration(duration)
    .attr("transform", function(d) { return "translate(" + d.y + "," + d.x + ")"; });

  nodeUpdate.select("circle")
    .attr("r", 10)
    .style("fill", function(d) { 
      if (d.is_content === true) {
        return "yellow"; 
      } else {
        return d._children ? "lightsteelblue" : "#fff"; 
      }
    });

  nodeUpdate.select("text")
    .style("fill-opacity", 1);

  // Transition exiting nodes to the parent's new position.
  var nodeExit = node.exit().transition()
    .duration(duration)
    .attr("transform", function(d) { 
      return "translate(" + source.y + "," + source.x + ")"; 
    })
    .remove();

  nodeExit.select("circle")
    .attr("r", 1e-6);

  nodeExit.select("text")
    .style("fill-opacity", 1e-6);

  // Update the links…
  var link = svg.selectAll("path.link")
    .data(links, function(d) { return d.target.id; });

  // Enter any new links at the parent's previous position.
  link.enter().insert("path", "g")
    .attr("class", "link")
    .attr("d", function(d) {
      var o = {x: source.x0, y: source.y0};
      return diagonal({source: o, target: o});
    });

  // Transition links to their new position.
  link.transition()
    .duration(duration)
    .attr("d", diagonal);

  // Transition exiting nodes to the parent's new position.
  link.exit().transition()
    .duration(duration)
    .attr("d", function(d) {
    var o = {x: source.x, y: source.y};
      return diagonal({source: o, target: o});
    })
    .remove();

  // Stash the old positions for transition.
  nodes.forEach(function(d) {
    d.x0 = d.x;
    d.y0 = d.y;
  });
}

// Toggle children on click.
function click(d) {
  if (d.children) {
    d._children = d.children;
    d.children = null;
  } else {
    d.children = d._children;
    d._children = null;
  }
  update(d);
}

/* original function */

function findAmongChildren(node, str) {
  for (var child in node["children"]) {
    if (node["children"][child]["name"] == str) {
      return node["children"][child];
    }
  }
  return null;
}

function insertToTree(data) {
  var dataName = data.getName();
  
  var idx = 0;
  var nameSize = dataName.size();

  var treeNode = root;
  var child = {};
  var changed = null;

  do {
    child = findAmongChildren(treeNode, dataName.get(idx).toEscapedString());
    if (child == null) {
      break;
    }
    idx ++;
    treeNode = child;
  } while (child !== null && idx < nameSize);

  for (var j = idx; j < nameSize; j++) {
    var newChild = {
      "name": dataName.get(j).toEscapedString(),
      "children": []
    };
    if (!("children" in treeNode)) {
      treeNode["children"] = [];
    }
    treeNode["children"].push(newChild);
    treeNode = newChild;
    if (changed === null) {
      changed = treeNode;
    }
  }
  
  // insert data content object if not present
  if (treeNode["children"].length === 0) {
    var content = data.getContent().buf().toString('binary');
    var contentNode = {
        "name": content,
        "is_content": true
      };
    // append to last treeNode
    treeNode["children"].push(contentNode);
  }
  
  if (changed !== null) {
    update(changed);
  }
}
