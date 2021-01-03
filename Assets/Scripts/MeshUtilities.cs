using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Helper class with Mesh utilities.
/// </summary>
/// 


// note pour Guillaume Levieux : classe empruntée à Brice qui m'a aidé
// je l'utilise pour que les mesh rouges générés dans MeleeWeapon soient clean niveau éclairage

public class MeshUtilities
{
	public static Mesh TranslateMesh(Vector3 p_translation, Mesh p_originalMesh)
	{
		Mesh l_translatedMesh = DuplicateMesh(p_originalMesh);

		for (int i = 0; i < l_translatedMesh.vertexCount; i++)
		{
			l_translatedMesh.vertices[i] = l_translatedMesh.vertices[i] + p_translation;
		}

		return l_translatedMesh;
	}

	public static Vector3 GetNormalFace(Vector3 l_vertexPositionA, Vector3 l_vertexPositionB, Vector3 l_vertexPositionC) // donne la normale à appliquer aux trois points du triangle
	{
		return Vector3.Cross(l_vertexPositionB - l_vertexPositionA, l_vertexPositionC - l_vertexPositionA)/*.normalized*/;
	}

	/// <summary>
	/// Combine meshes, keep vertices position, triangle, color and normal. But recalcule bounds and tangents.
	/// </summary>
	/// <param name="l_meshs"></param>
	/// <param name="p_oneMesh"></param>
	/// <returns></returns>
	public static Mesh CombineMeshes(bool p_oneMesh, params Mesh[] l_meshs)
	{
		CombineInstance[] l_combines = new CombineInstance[l_meshs.Length];
		for (int i = 0; i < l_meshs.Length; i++)
		{
			l_combines[i].mesh = l_meshs[i];
		}

		Mesh l_mesh = new Mesh();
		l_mesh.CombineMeshes(l_combines, p_oneMesh, false, false);

		l_mesh.RecalculateBounds();
		l_mesh.RecalculateTangents();
		return l_mesh;
	}

	/// <summary>
	/// Unifies the meshes. And delete duplicate vertices.
	/// </summary>
	/// <returns>The meshes.</returns>
	/// <param name="meshes">Meshes.</param>
	public static Mesh UnifyMeshes(params Mesh[] meshes)
	{
		Mesh combinedMesh = new Mesh();

		CombineInstance[] combine = new CombineInstance[meshes.Length];

		for (int i = 0; i < meshes.Length; i++)
		{
			combine[i].mesh = meshes[i];
		}

		combinedMesh.CombineMeshes(combine, true, false);

		List<Vector3> vertices = combinedMesh.vertices.ToList();
		List<int> triangles = new List<int>();

		vertices = vertices.Distinct().ToList();

		for (int i = 0; i < combinedMesh.triangles.Length; i++)
		{
			Vector3 v1 = combinedMesh.vertices[combinedMesh.triangles[i]];

			foreach (Vector3 v2 in vertices)
			{
				if (v1.Equals(v2))
				{
					triangles.Add(vertices.IndexOf(v2));
					break;
				}
			}
		}

		Mesh mesh = new Mesh();
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();

		return mesh;
	}

	/// <summary>
	/// Define the uv of the mesh, keep normal, vertice position and triangle, but recalculate tangents.
	/// Will create three vertices for each triangle and copy source information on duplicate vertice.
	/// </summary>
	/// <param name="p_mesh">Mesh.</param>
	/// <param name="p_offset">The scale of the texture.</param>
	/// <param name="p_scale">The offset of the texture.</param>
	/// <returns>A duplication of the source mesh.</returns>
	public static Mesh PerfectUVMesh(Mesh p_mesh, float p_scale, float p_offset)
	{
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		List<Vector3> l_normals = new List<Vector3>();
		List<Vector2> l_uvs = new List<Vector2>();
		int l_verticeCount = 0;

		for (int i = 0; i < p_mesh.triangles.Length; i += 3)
		{
			int l_indexA = p_mesh.triangles[i + 0];
			int l_indexB = p_mesh.triangles[i + 1];
			int l_indexC = p_mesh.triangles[i + 2];

			Vector3 l_verticePosA = p_mesh.vertices[l_indexA];
			Vector3 l_verticePosB = p_mesh.vertices[l_indexB];
			Vector3 l_verticePosC = p_mesh.vertices[l_indexC];

			Vector3 l_normal = GetNormalFace(l_verticePosA, l_verticePosB, l_verticePosC);
			//if triangle is not useless		
			if (l_normal != Vector3.zero)
			{
				// Form a rotation that points the z+ axis in this perpendicular direction.
				// Multiplying by the inverse will flatten the triangle into an xy plane.
				Quaternion l_triangleRotation = Quaternion.Inverse(Quaternion.LookRotation(l_normal));

				var l_offset = new Vector2(p_offset, p_offset);

				vertices.Add(l_verticePosA);
				triangles.Add(l_verticeCount++);
				l_normals.Add(p_mesh.normals[l_indexA]);
				l_uvs.Add((Vector2)(l_triangleRotation * l_verticePosA) * p_scale + l_offset);

				vertices.Add(l_verticePosB);
				triangles.Add(l_verticeCount++);
				l_normals.Add(p_mesh.normals[l_indexB]);
				l_uvs.Add((Vector2)(l_triangleRotation * l_verticePosB) * p_scale + l_offset);

				vertices.Add(l_verticePosC);
				triangles.Add(l_verticeCount++);
				l_normals.Add(p_mesh.normals[l_indexC]);
				l_uvs.Add((Vector2)(l_triangleRotation * l_verticePosC) * p_scale + l_offset);
			}
		}

		var l_mesh = new Mesh
		{
			vertices = vertices.ToArray(),
			triangles = triangles.ToArray(),
			normals = l_normals.ToArray(),
			uv = l_uvs.ToArray(),
			name = p_mesh.name
		};

		l_mesh.RecalculateTangents();
		return l_mesh;
	}

	/// <summary>
	/// Creates a primitive mesh. DEPRECATED.
	/// </summary>
	/// <returns>The primitive mesh.</returns>
	/// <param name="type">Type of primitive.</param>
	public static Mesh CreatePrimitiveMesh(PrimitiveType type)
	{
		GameObject gameObject = GameObject.CreatePrimitive(type);

		Mesh defaultMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
		Mesh mesh = new Mesh();

		mesh.vertices = defaultMesh.vertices;
		mesh.triangles = defaultMesh.triangles;
		mesh.normals = defaultMesh.normals;
		mesh.tangents = defaultMesh.tangents;
		mesh.colors = defaultMesh.colors;
		mesh.uv = defaultMesh.uv;
		mesh.name = type.ToString();

		Object.DestroyImmediate(gameObject);

		return mesh;
	}

	/// <summary>
	/// Duplicates a mesh.
	/// </summary>
	/// <returns>Duplicated mesh.</returns>
	/// <param name="mesh">Mesh.</param>
	public static Mesh DuplicateMesh(Mesh mesh)
	{
		Mesh duplicateMesh = new Mesh
		{
			vertices = mesh.vertices,
			triangles = mesh.triangles,
			normals = mesh.normals,
			tangents = mesh.tangents,
			colors = mesh.colors,
			uv = mesh.uv,
			name = mesh.name
		};

		return duplicateMesh;
	}

	/// <summary>
	/// Gets a random point on mesh.
	/// </summary>
	/// <returns>The random point on mesh.</returns>
	/// <param name="mesh">Mesh.</param>
	public static Vector3 GetRandomPointOnMesh(Mesh mesh)
	{
		int triangleCount = mesh.triangles.Length / 3;
		float[] sizes = new float[triangleCount];
		for (int i = 0; i < triangleCount; i++)
		{
			Vector3 va = mesh.vertices[mesh.triangles[i * 3 + 0]];
			Vector3 vb = mesh.vertices[mesh.triangles[i * 3 + 1]];
			Vector3 vc = mesh.vertices[mesh.triangles[i * 3 + 2]];

			sizes[i] = .5f * Vector3.Cross(vb - va, vc - va).magnitude;
		}

		// if you're repeatedly doing this on a single mesh, you'll likely want to cache cumulativeSizes and total
		float[] cumulativeSizes = new float[sizes.Length];
		float total = 0;

		for (int i = 0; i < sizes.Length; i++)
		{
			total += sizes[i];
			cumulativeSizes[i] = total;
		}

		// so everything above this point wants to be factored out
		float randomsample = Random.value * total;

		int triIndex = -1;

		for (int i = 0; i < sizes.Length; i++)
		{
			if (randomsample <= cumulativeSizes[i])
			{
				triIndex = i;
				break;
			}
		}

		if (triIndex == -1) Debug.LogError("triIndex should never be -1");

		Vector3 a = mesh.vertices[mesh.triangles[triIndex * 3 + 0]];
		Vector3 b = mesh.vertices[mesh.triangles[triIndex * 3 + 1]];
		Vector3 c = mesh.vertices[mesh.triangles[triIndex * 3 + 2]];

		// generate random barycentric coordinates
		float r = Random.value;
		float s = Random.value;

		if (r + s >= 1)
		{
			r = 1 - r;
			s = 1 - s;
		}
		// and then turn them back to a Vector3
		Vector3 pointOnMesh = a + r * (b - a) + s * (c - a);
		return pointOnMesh;
	}

	/// <summary>
	/// converts Mesh to string.
	/// </summary>
	/// <returns>The Mesh as string.</returns>
	/// <param name="mesh">Mesh.</param>
	public static string MeshToString(Mesh mesh)
	{
		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.Append("g Exported Mesh").Append("\n");
		foreach (Vector3 v in mesh.vertices)
		{
			stringBuilder.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, v.z));
		}
		stringBuilder.Append("\n");
		foreach (Vector3 v in mesh.normals)
		{
			stringBuilder.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
		}
		stringBuilder.Append("\n");
		foreach (Vector3 v in mesh.uv)
		{
			stringBuilder.Append(string.Format("vt {0} {1}\n", v.x, v.y));
		}

		int[] triangles = mesh.triangles;
		for (int i = 0; i < triangles.Length; i += 3)
		{
			stringBuilder.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
				triangles[i] + 1, triangles[i + 1] + 1, triangles[i + 2] + 1));
		}

		return stringBuilder.ToString();
	}

	/// <summary>
	/// Exports Mesh to object.
	/// </summary>
	/// <param name="mesh">Mesh.</param>
	/// <param name="filename">Filename.</param>
	public static void ExportToOBJ(Mesh mesh, string filename)
	{
		string extension = Path.GetExtension(filename);

		if (extension.ToLower() != ".obj")
		{
			filename += ".obj";
		}

		using (StreamWriter sw = new StreamWriter(filename))
		{
			sw.Write(MeshToString(mesh));
		}
	}


	/// <summary>
	/// Generates the mesh with low-poly rendering. 
	/// </summary>
	public static Mesh SetupFlatMesh(Mesh p_mesh)
	{
		int l_triangleCount = p_mesh.triangles.Length / 3;

		var l_vertices = new List<Vector3>(p_mesh.vertexCount);
		var l_triangles = new List<int>(l_triangleCount * 3);
		var l_normals = new List<Vector3>(p_mesh.vertexCount);

		int l_vertexCount = 0;
		for (int i = 0; i < l_triangleCount; i++)
		{
			// Get the three vertices of triangle
			int l_triangleIndex = i * 3;
			int l_vertexIndexA = p_mesh.triangles[l_triangleIndex];
			int l_vertexIndexB = p_mesh.triangles[l_triangleIndex + 1];
			int l_vertexIndexC = p_mesh.triangles[l_triangleIndex + 2];

			Vector3 l_vertexPositionA = p_mesh.vertices[l_vertexIndexA];
			Vector3 l_vertexPositionB = p_mesh.vertices[l_vertexIndexB];
			Vector3 l_vertexPositionC = p_mesh.vertices[l_vertexIndexC];

			//Vertices
			l_vertices.Add(l_vertexPositionA);
			l_vertices.Add(l_vertexPositionB);
			l_vertices.Add(l_vertexPositionC);

			//Triangles
			l_triangles.Add(l_vertexCount++);
			l_triangles.Add(l_vertexCount++);
			l_triangles.Add(l_vertexCount++);

			// Compute a vector perpendicular to the face.
			// NORMAL
			Vector3 l_normal = MeshUtilities.GetNormalFace(l_vertexPositionA, l_vertexPositionB, l_vertexPositionC);
			l_normals.Add(l_normal);
			l_normals.Add(l_normal);
			l_normals.Add(l_normal);
		}
		var l_completeMesh = new Mesh();
		l_completeMesh.vertices = l_vertices.ToArray();
		l_completeMesh.triangles = l_triangles.ToArray();
		l_completeMesh.normals = l_normals.ToArray();

		l_completeMesh = MeshUtilities.PerfectUVMesh(l_completeMesh, 1, 1);

		l_completeMesh.RecalculateBounds();
		//save this mesh for next generation
		l_completeMesh.name = "CompleteMesh";
		return l_completeMesh;
	}

	/// <summary>
	/// Generates the mesh with smooth rendering.
	/// </summary>
	public static Mesh SetupSmoothMesh(Mesh p_mesh)
	{
		Mesh l_completeMesh = p_mesh;

		int l_vertexCount = l_completeMesh.vertices.Length;
		int l_triangleCount = l_completeMesh.triangles.Length / 3;

		var l_normals = new Vector3[l_vertexCount];

		Vector3 l_zero = Vector3.zero;

		for (int i = 0; i < l_triangleCount; i++)
		{
			// Get the three vertices of triangle
			int l_triangleIndex = i * 3;
			int l_vertexIndexA = l_completeMesh.triangles[l_triangleIndex];
			int l_vertexIndexB = l_completeMesh.triangles[l_triangleIndex + 1];
			int l_vertexIndexC = l_completeMesh.triangles[l_triangleIndex + 2];

			Vector3 l_vertexPositionA = l_completeMesh.vertices[l_vertexIndexA];
			Vector3 l_vertexPositionB = l_completeMesh.vertices[l_vertexIndexB];
			Vector3 l_vertexPositionC = l_completeMesh.vertices[l_vertexIndexC];

			// Compute a vector perpendicular to the face.
			////NORMAL
			Vector3 l_normal = MeshUtilities.GetNormalFace(l_vertexPositionA, l_vertexPositionB, l_vertexPositionC);
			if (l_normal.y >= l_zero.y)
			{
				l_normals[l_vertexIndexA] += l_normal;
				l_normals[l_vertexIndexB] += l_normal;
				l_normals[l_vertexIndexC] += l_normal;
			}
		}
		l_completeMesh.normals = l_normals;

		//l_completeMesh = MeshUtilities.PerfectUVMesh(l_completeMesh, 1, 1);

		l_completeMesh.RecalculateBounds();
		//save this mesh for next generation
		l_completeMesh.name = "CompleteMesh";
		
		return l_completeMesh;
	}
}
